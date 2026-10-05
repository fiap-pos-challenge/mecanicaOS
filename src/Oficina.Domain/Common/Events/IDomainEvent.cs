namespace Oficina.Domain.Common.Events;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
