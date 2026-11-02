using Oficina.Domain.Inventory.Enums;

namespace Oficina.Application.Inventory.Dtos;

public sealed record PartDetailDto(
    Guid Id,
    string Code,
    string Name,
    PartType Type,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal StockQuantity,
    decimal MinimumStock,
    bool IsBelowMinimum,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<StockMovementDto> StockMovements);
