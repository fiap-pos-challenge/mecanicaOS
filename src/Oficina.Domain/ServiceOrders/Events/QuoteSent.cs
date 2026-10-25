using Oficina.Domain.Common.Events;

namespace Oficina.Domain.ServiceOrders.Events;

public sealed record QuoteSent(Guid ServiceOrderId, long Number, int QuoteVersion, decimal Total, DateTimeOffset OccurredAt) : IDomainEvent;
