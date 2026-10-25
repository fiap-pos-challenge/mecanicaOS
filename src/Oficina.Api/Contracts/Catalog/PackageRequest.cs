namespace Oficina.Api.Contracts.Catalog;

public sealed record PackageRequest(
    string Name,
    string? Description,
    IReadOnlyList<PackageItemBody> Items,
    decimal Price,
    PackageSuggestionBody? Suggestion);
