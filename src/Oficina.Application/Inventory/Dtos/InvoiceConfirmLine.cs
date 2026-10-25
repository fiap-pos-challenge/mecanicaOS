namespace Oficina.Application.Inventory.Dtos;

public sealed record InvoiceConfirmLine(Guid PartId, decimal Quantity, decimal UnitCost);
