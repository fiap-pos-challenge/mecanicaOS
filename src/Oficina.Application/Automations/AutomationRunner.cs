using Microsoft.Extensions.Logging;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Automations.Dtos;
using Oficina.Application.Automations.Queries;
using Oficina.Application.Inventory;
using Oficina.Application.Notifications;
using Oficina.Domain.Automations;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations;

public sealed partial class AutomationRunner(
    IAutomationRepository automations,
    IAutomationQueries queries,
    AutomationScheduler scheduler,
    ITeamNotificationPublisher notifications,
    PurchaseOrderDrafts purchaseOrders,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<AutomationRunner> logger)
{
    public const int BatchSize = 50;

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        foreach (var subject in await queries.ScanAsync(timeProvider.GetUtcNow(), cancellationToken))
        {
            await scheduler.ScheduleAsync(subject.Trigger, subject.SubjectId, subject.DedupeKey, cancellationToken);
        }

        if (!await CommitAsync(cancellationToken))
        {
            return 0;
        }

        var due = await automations.DueAsync(timeProvider.GetUtcNow(), BatchSize, cancellationToken);
        var finished = 0;
        foreach (var execution in due)
        {
            await ExecuteAsync(execution, cancellationToken);
            if (!await CommitAsync(cancellationToken))
            {
                return finished;
            }

            finished++;
        }

        return finished;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Automation queued a WhatsApp message for {Trigger}")]
    private static partial void LogWhatsApp(ILogger logger, string trigger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Automation commit failed: {ErrorCode}")]
    private static partial void LogCommitFailed(ILogger logger, string errorCode);

    private async Task<bool> CommitAsync(CancellationToken cancellationToken)
    {
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } error)
        {
            LogCommitFailed(logger, error.Code);
            return false;
        }

        return true;
    }

    private async Task ExecuteAsync(AutomationExecution execution, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (await automations.GetRuleAsync(execution.RuleId, cancellationToken) is not { Enabled: true } rule)
        {
            execution.Finish(ExecutionStatus.Skipped, AutomationMessages.RuleOff, null, now);
            return;
        }

        var definition = AutomationDefinitions.Read(rule.DefinitionJson);
        if (await queries.ContextAsync(execution.Trigger, execution.SubjectId, cancellationToken) is not { } context)
        {
            execution.Finish(ExecutionStatus.Skipped, AutomationMessages.NotFound(definition.Name), null, now);
            return;
        }

        var values = AutomationContextValues.From(context);
        if (!definition.Conditions.All(condition => AutomationDefinitions.Matches(condition, values)))
        {
            execution.Finish(ExecutionStatus.Skipped, AutomationMessages.ConditionsNotMet(definition.Name), null, now);
            return;
        }

        var done = new List<string>();
        foreach (var action in definition.Actions)
        {
            var outcome = await ActAsync(action, context, execution.Trigger, cancellationToken);
            if (outcome.Error is { } error)
            {
                var subject = context.Number is { } number ? $" · OS {number}" : context.PartName is { } part ? $" · {part}" : string.Empty;
                execution.Finish(ExecutionStatus.Failed, $"{definition.Name}{subject}", error.Message, now);
                await notifications.PublishAsync(NotificationTypes.AutomationFailed, AutomationMessages.FailureTitle(definition.Name), AutomationMessages.FailureBody(error.Message), "/automations", false, null, cancellationToken);
                return;
            }

            done.Add(outcome.Value);
        }

        execution.Finish(ExecutionStatus.Done, AutomationMessages.Done(definition.Name, done), null, now);
    }

    private async Task<Result<string>> ActAsync(RuleActionDto action, AutomationContext context, string trigger, CancellationToken cancellationToken)
    {
        var values = AutomationContextValues.From(context);
        switch (action.Kind)
        {
            case AutomationActions.SendEmail:
                if (!context.ConsentEmail || context.Email is null)
                {
                    return AutomationMessages.EmailNoConsent;
                }

                var subject = context.Number is { } number ? AutomationMessages.Order(number) : AutomationMessages.DefaultSubject;
                var sent = await emailSender.SendAsync(new EmailMessage(context.Email, subject, $"<p>{System.Net.WebUtility.HtmlEncode(AutomationDefinitions.Render(action.Message!, values))}</p>"), cancellationToken);
                return sent ? AutomationMessages.EmailSent : AutomationErrors.EmailFailed;
            case AutomationActions.SendWhatsApp:
                if (!context.ConsentWhatsApp || context.Phone is null)
                {
                    return AutomationMessages.WhatsAppNoConsent;
                }

                LogWhatsApp(logger, trigger);
                return AutomationMessages.WhatsAppQueued;
            case AutomationActions.NotifyTeam:
                var type = trigger switch
                {
                    AutomationTriggers.StockBelowMinimum => NotificationTypes.StockLow,
                    AutomationTriggers.PaymentReceived => NotificationTypes.PaymentConfirmed,
                    AutomationTriggers.QuoteSent => NotificationTypes.QuoteAnswered,
                    _ => NotificationTypes.ServiceOrderChanged,
                };
                await notifications.PublishAsync(type, AutomationDefinitions.Render(action.Message!, values), context.Number is { } os ? AutomationMessages.Order(os) : context.PartName ?? string.Empty,
                    context.ServiceOrderId is null ? "/inventory" : "/service-orders", false, null, cancellationToken);
                return AutomationMessages.TeamNotified;
            case AutomationActions.CreatePurchaseOrder:
                if (context.PartId is not { } partId)
                {
                    return AutomationErrors.NoPart;
                }

                var order = await purchaseOrders.CreateAsync(partId, cancellationToken);
                return order.Error is { } error ? error : AutomationMessages.PurchaseOrderCreated(order.Value.Number);
            default:
                return AutomationMessages.PaymentRegistered;
        }
    }
}
