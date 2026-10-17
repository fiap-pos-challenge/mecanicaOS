using Oficina.Domain.Catalog.Enums;

namespace Oficina.Application.Catalog.Dtos;

public sealed record PackageItemDto(CatalogItemKind Kind, Guid ItemId, decimal Quantity, string Name, decimal UnitPrice);

public sealed record PackageSuggestionDto(int? MinMileage, int? MaxMileage);

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

public sealed record PackageItemRequest(CatalogItemKind Kind, Guid ItemId, decimal Quantity);
