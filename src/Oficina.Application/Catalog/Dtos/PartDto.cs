using Oficina.Domain.Inventory.Enums;

namespace Oficina.Application.Catalog.Dtos;

public sealed record PartDto(
    Guid Id,
    string Code,
    string Name,
    PartType Type,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal? Cost,
    decimal StockQuantity,
    decimal MinimumStock,
    string? Supplier,
    string? Barcode,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
