namespace Oficina.Domain.Inventory.Entities;

public sealed record PurchaseOrderLine(Guid PartId, decimal Quantity, decimal UnitCost);
