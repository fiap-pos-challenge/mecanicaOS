namespace Oficina.Application.Analytics;

public sealed record LowStockFact(Guid PartId, string Name, decimal Stock, decimal Minimum, string UnitOfMeasure, int DependentOrders);
