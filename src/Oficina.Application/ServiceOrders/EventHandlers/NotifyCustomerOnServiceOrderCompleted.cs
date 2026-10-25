using Oficina.Application.Abstractions.Messaging;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Domain.ServiceOrders.Events;

namespace Oficina.Application.ServiceOrders.EventHandlers;

public sealed class NotifyCustomerOnServiceOrderCompleted(INotifier notifier) : IDomainEventHandler<ServiceOrderCompleted>
{
    public Task HandleAsync(ServiceOrderCompleted domainEvent, CancellationToken cancellationToken) =>
        notifier.NotifyServiceOrderCompletedAsync(domainEvent.Number, cancellationToken);
}
