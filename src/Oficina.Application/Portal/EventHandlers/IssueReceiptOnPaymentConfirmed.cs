using Oficina.Application.Abstractions.Messaging;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Automations;
using Oficina.Application.Documents;
using Oficina.Application.Notifications;
using Oficina.Application.Quotes.Pricing;
using Oficina.Application.Quotes.Queries;
using Oficina.Domain.Portal.Events;
using Oficina.Domain.Quotes.Repositories;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Application.Portal.EventHandlers;

public sealed class IssueReceiptOnPaymentConfirmed(
    IServiceOrderRepository serviceOrders,
    IEstimateRepository estimates,
    IQuoteQueries quoteQueries,
    QuotePricing pricing,
    DocumentIssuer documents,
    ITeamNotificationPublisher notifications,
    AutomationScheduler automations,
    IUnitOfWork unitOfWork) : IDomainEventHandler<PaymentConfirmed>
{
    public async Task HandleAsync(PaymentConfirmed domainEvent, CancellationToken cancellationToken)
    {
        if (await serviceOrders.GetByIdAsync(domainEvent.ServiceOrderId, cancellationToken) is not { } order || await quoteQueries.ContextAsync(order.Id, cancellationToken) is not { } context)
        {
            return;
        }

        var estimate = await estimates.GetByIdAsync(domainEvent.EstimateId, cancellationToken);
        var totals = estimate?.Price(await pricing.TermsAsync(context.Facts, cancellationToken));
        await documents.IssueAsync(DocumentIssuer.ReceiptKind, order, context.CustomerName, $"{context.LicensePlate} · {context.Vehicle}", estimate, totals, domainEvent.OccurredAt, cancellationToken);
        await notifications.PublishAsync(NotificationTypes.PaymentConfirmed, PortalTexts.PaymentTitle(order.Number.Value), PortalTexts.PaymentBody, "/service-orders", false, null, cancellationToken);
        await automations.ScheduleAsync(AutomationTriggers.PaymentReceived, order.Id, $"payment:{domainEvent.ChargeId:N}", cancellationToken);
        await unitOfWork.CommitOrThrowAsync(cancellationToken);
    }
}
