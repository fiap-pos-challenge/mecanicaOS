namespace Oficina.Application.Catalog.Dtos;

public sealed record ServiceDto(
    Guid Id,
    string Name,
    string? Description,
    decimal BasePrice,
    int EstimatedMinutes,
    decimal CommissionPercent,
    int WarrantyDays,
    IReadOnlyList<UsualPartDto> UsualParts,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
