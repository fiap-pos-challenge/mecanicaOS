namespace Oficina.Application.Catalog.Dtos;

public sealed record ServiceDto(
    Guid Id,
    string Name,
    string? Description,
    decimal BasePrice,
    int EstimatedMinutes,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
