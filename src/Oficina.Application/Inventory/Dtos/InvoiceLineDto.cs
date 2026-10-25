namespace Oficina.Application.Inventory.Dtos;

public sealed record InvoiceLineDto(string Description, string? Barcode, Guid? PartId, decimal Quantity, decimal UnitCost);
