using Oficina.Domain.Common.Events;

namespace Oficina.Domain.Inventory.Events;

public sealed record StockBelowMinimum(Guid PartId, string PartName, decimal Stock, decimal MinimumStock, DateTimeOffset OccurredAt) : IDomainEvent;
