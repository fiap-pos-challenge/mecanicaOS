using Oficina.Domain.Common.Events;

namespace Oficina.Application.Abstractions.Messaging;

public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
