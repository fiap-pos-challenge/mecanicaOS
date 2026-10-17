using Oficina.Domain.Catalog.Enums;

namespace Oficina.Api.Contracts.Catalog;

public sealed record PackageItemBody(CatalogItemKind Kind, Guid ItemId, decimal Quantity);

public sealed record PackageSuggestionBody(int? MinMileage, int? MaxMileage);

public sealed record PackageRequest(
    string Name,
    string? Description,
    IReadOnlyList<PackageItemBody> Items,
    decimal Price,
    PackageSuggestionBody? Suggestion);
