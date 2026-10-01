using Oficina.Domain.Common.Events;

namespace Oficina.Domain.ServiceOrders.Events;

public sealed record ServiceOrderCompleted(Guid ServiceOrderId, long Number, Guid CustomerId, DateTimeOffset OccurredAt) : IDomainEvent;
