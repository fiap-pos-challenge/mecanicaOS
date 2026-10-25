namespace Oficina.Application.Inventory.Invoices;

public sealed record NfeLine(string Description, string? Barcode, decimal Quantity, decimal UnitCost);
