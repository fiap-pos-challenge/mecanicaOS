using Oficina.Application.Abstractions.Messaging;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Realtime;
using Oficina.Application.Notifications;
using Oficina.Domain.ServiceOrders.Events;

namespace Oficina.Application.Automations.EventHandlers;

public sealed class OnServiceOrderStatusChanged(AutomationScheduler automations, ITeamNotificationPublisher notifications, IRealtimePublisher realtime, IUnitOfWork unitOfWork)
    : IDomainEventHandler<ServiceOrderStatusChanged>
{
    public async Task HandleAsync(ServiceOrderStatusChanged domainEvent, CancellationToken cancellationToken)
    {
        await realtime.PublishAsync(new { type = "serviceOrderStatusChanged", serviceOrderId = domainEvent.ServiceOrderId, number = domainEvent.Number, status = domainEvent.Status, occurredAt = domainEvent.OccurredAt }, cancellationToken);
        await automations.ScheduleAsync(AutomationTriggers.ServiceOrderStatusChanged, domainEvent.ServiceOrderId, $"status:{domainEvent.ServiceOrderId:N}:{domainEvent.Status}:{domainEvent.OccurredAt.ToUnixTimeSeconds()}", cancellationToken);
        if (domainEvent.Status == "Delivered")
        {
            await automations.ScheduleAsync(AutomationTriggers.ServiceOrderDelivered, domainEvent.ServiceOrderId, $"delivered:{domainEvent.ServiceOrderId:N}", cancellationToken);
        }

        if (domainEvent.Status is "Completed" or "WaitingApproval")
        {
            var title = domainEvent.Status == "Completed" ? AutomationEventTexts.Ready(domainEvent.Number) : AutomationEventTexts.WaitingApproval(domainEvent.Number);
            await notifications.PublishAsync(NotificationTypes.ServiceOrderChanged, title, AutomationEventTexts.OpenOrder, "/service-orders", false, null, cancellationToken);
        }

        await unitOfWork.CommitOrThrowAsync(cancellationToken);
    }
}
