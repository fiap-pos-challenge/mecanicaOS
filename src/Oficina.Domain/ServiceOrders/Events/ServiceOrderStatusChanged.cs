using Oficina.Domain.Common.Events;

namespace Oficina.Domain.ServiceOrders.Events;

public sealed record ServiceOrderStatusChanged(Guid ServiceOrderId, long Number, string Status, DateTimeOffset OccurredAt) : IDomainEvent;
