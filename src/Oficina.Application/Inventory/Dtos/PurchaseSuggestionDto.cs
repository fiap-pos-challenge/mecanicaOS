namespace Oficina.Application.Inventory.Dtos;

public sealed record PurchaseSuggestionDto(
    Guid PartId,
    string Code,
    string Name,
    string Type,
    string UnitOfMeasure,
    Guid? SupplierId,
    decimal StockQuantity,
    decimal MinimumStock,
    decimal Forecast30d,
    decimal SuggestedQuantity,
    decimal UnitCost);
