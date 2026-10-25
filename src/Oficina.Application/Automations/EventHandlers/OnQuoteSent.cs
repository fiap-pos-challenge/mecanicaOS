using Oficina.Application.Abstractions.Messaging;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Domain.ServiceOrders.Events;

namespace Oficina.Application.Automations.EventHandlers;

public sealed class OnQuoteSent(AutomationScheduler automations, IUnitOfWork unitOfWork) : IDomainEventHandler<QuoteSent>
{
    public async Task HandleAsync(QuoteSent domainEvent, CancellationToken cancellationToken)
    {
        await automations.ScheduleAsync(AutomationTriggers.QuoteSent, domainEvent.ServiceOrderId, $"quote:{domainEvent.ServiceOrderId:N}:{domainEvent.QuoteVersion}:{domainEvent.OccurredAt.ToUnixTimeSeconds()}", cancellationToken);
        await unitOfWork.CommitOrThrowAsync(cancellationToken);
    }
}
