namespace Oficina.Api.Contracts.Inventory;

public sealed record StockExitRequest(Guid PartId, decimal Quantity, string? Reason, long? ServiceOrderNumber);
