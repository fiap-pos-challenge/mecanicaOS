using Oficina.Domain.Common.Events;

namespace Oficina.Domain.Inventory.Events;

public sealed record StockDeducted(Guid PartId, decimal Quantity, decimal Remaining, Guid ServiceOrderId, long ServiceOrderNumber, DateTimeOffset OccurredAt) : IDomainEvent;
