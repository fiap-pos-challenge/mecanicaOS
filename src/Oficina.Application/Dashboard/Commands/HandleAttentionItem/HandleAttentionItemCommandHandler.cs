using MediatR;
using Microsoft.Extensions.Logging;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Analytics;
using Oficina.Application.Inventory;
using Oficina.Application.Quotes.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Dashboard;
using Oficina.Domain.Quotes.Repositories;

namespace Oficina.Application.Dashboard.Commands.HandleAttentionItem;

public sealed partial class HandleAttentionItemCommandHandler(
    IAnalyticsQueries analytics,
    IDashboardActionRepository actions,
    PurchaseOrderDrafts purchaseOrders,
    IEstimateRepository estimates,
    IQuoteQueries quoteQueries,
    IEmailSender emailSender,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<HandleAttentionItemCommandHandler> logger) : IRequestHandler<HandleAttentionItemCommand, Result>
{
    public async Task<Result> Handle(HandleAttentionItemCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var operations = await analytics.OperationsAsync(now, cancellationToken);
        var handled = await actions.HandledSinceAsync(now - DashboardAttention.HideHandledFor, cancellationToken);
        if (DashboardAttention.Items(operations, now).FirstOrDefault(item => item.Id == command.Id && !handled.Contains(item.Id)) is not { } item)
        {
            return DashboardErrors.AttentionNotFound;
        }

        if (item.Action is null || item.Action != command.Action)
        {
            return DashboardErrors.ActionNotAllowed;
        }

        EmailMessage? email = null;
        if (command.Action == DashboardAttention.CreatePurchaseOrder)
        {
            var part = operations.LowStock.First(fact => DashboardAttention.StockId(fact.PartId) == command.Id);
            var order = await purchaseOrders.CreateAsync(part.PartId, cancellationToken);
            if (order.Error is { } orderError)
            {
                return orderError;
            }
        }
        else
        {
            email = command.Action == DashboardAttention.SendReminder
                ? await ReminderAsync(operations.PendingQuotes.First(fact => DashboardAttention.QuoteId(fact.EstimateId) == command.Id), cancellationToken)
                : await DeadlineAsync(operations.OpenOrders.First(fact => DashboardAttention.LateId(fact.Id) == command.Id), cancellationToken);
        }

        actions.Add(DashboardAction.Record(command.Id, command.Action, currentUser.UserId, now));
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        if (email is null)
        {
            if (command.Action != DashboardAttention.CreatePurchaseOrder)
            {
                LogQueued(logger, command.Action);
            }

            return Result.Success();
        }

        await emailSender.SendAsync(email, cancellationToken);
        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Dashboard action {Action} queued for WhatsApp")]
    private static partial void LogQueued(ILogger logger, string action);

    private async Task<EmailMessage?> ReminderAsync(PendingQuoteFact fact, CancellationToken cancellationToken) =>
        await estimates.GetByIdAsync(fact.EstimateId, cancellationToken) is { Delivery: { } delivery } estimate
        && await quoteQueries.ContextAsync(estimate.ServiceOrderId, cancellationToken) is { ConsentEmail: true, Email: { } address } context
            ? DashboardEmails.Reminder(address, context, estimate.Number, delivery.Link)
            : null;

    private async Task<EmailMessage?> DeadlineAsync(OpenOrderFact fact, CancellationToken cancellationToken) =>
        await quoteQueries.ContextAsync(fact.Id, cancellationToken) is { ConsentEmail: true, Email: { } address } context
            ? DashboardEmails.NewDeadline(address, context, fact.Number)
            : null;
}
