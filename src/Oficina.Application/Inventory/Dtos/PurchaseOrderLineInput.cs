namespace Oficina.Application.Inventory.Dtos;

public sealed record PurchaseOrderLineInput(Guid PartId, decimal Quantity, decimal UnitCost);
