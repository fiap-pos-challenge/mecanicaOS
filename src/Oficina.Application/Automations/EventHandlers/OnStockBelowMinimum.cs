using Oficina.Application.Abstractions.Messaging;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Notifications;
using Oficina.Domain.Inventory.Events;

namespace Oficina.Application.Automations.EventHandlers;

public sealed class OnStockBelowMinimum(AutomationScheduler automations, ITeamNotificationPublisher notifications, IUnitOfWork unitOfWork) : IDomainEventHandler<StockBelowMinimum>
{
    public async Task HandleAsync(StockBelowMinimum domainEvent, CancellationToken cancellationToken)
    {
        var dedupeKey = AutomationTriggers.StockDedupeKey(domainEvent.PartId, domainEvent.OccurredAt);
        await automations.ScheduleAsync(AutomationTriggers.StockBelowMinimum, domainEvent.PartId, dedupeKey, cancellationToken);
        await notifications.PublishAsync(NotificationTypes.StockLow, AutomationEventTexts.StockLow(domainEvent.PartName), AutomationEventTexts.StockLowBody, "/inventory", false, dedupeKey, cancellationToken);
        await unitOfWork.CommitOrThrowAsync(cancellationToken);
    }
}
