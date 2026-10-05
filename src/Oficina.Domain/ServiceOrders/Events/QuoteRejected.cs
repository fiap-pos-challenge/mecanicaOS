using Oficina.Domain.Common.Events;

namespace Oficina.Domain.ServiceOrders.Events;

public sealed record QuoteRejected(Guid ServiceOrderId, long Number, DateTimeOffset OccurredAt) : IDomainEvent;
