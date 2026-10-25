namespace Oficina.Application.Inventory.Dtos;

public sealed record StockItemDto(
    Guid PartId,
    string Code,
    string Name,
    string Type,
    string UnitOfMeasure,
    string Location,
    decimal StockQuantity,
    decimal MinimumStock,
    decimal Outflow30d,
    decimal UnitCost,
    DateTimeOffset? LastOutflowAt,
    string? Barcode,
    string Situation);
