namespace Oficina.Application.Inventory.Dtos;

public sealed record PurchaseOrderLineDto(Guid PartId, string Code, string Name, decimal Quantity, decimal UnitCost);
