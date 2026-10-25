namespace Oficina.Application.Catalog.Dtos;

public sealed record PackageDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<PackageItemDto> Items,
    decimal Price,
    decimal ItemsTotal,
    decimal Savings,
    PackageSuggestionDto? Suggestion,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
