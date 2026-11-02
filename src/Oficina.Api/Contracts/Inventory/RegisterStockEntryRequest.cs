namespace Oficina.Api.Contracts.Inventory;

public sealed record RegisterStockEntryRequest(decimal Quantity, string Reason);
