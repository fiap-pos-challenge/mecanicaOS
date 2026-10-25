namespace Oficina.Api.Contracts.Inventory;

public sealed record StockEntryRequest(Guid PartId, decimal Quantity, decimal UnitCost, string? Reason, Guid? SupplierId, string? InvoiceNumber);
