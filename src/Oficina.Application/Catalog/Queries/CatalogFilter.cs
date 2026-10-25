using Oficina.Application.Catalog.Dtos;

namespace Oficina.Application.Catalog.Queries;

public sealed record CatalogFilter(string? Term, bool? IsActive)
{
    public static CatalogFilter From(string? term, CatalogStatus? status, bool includeInactive) => new(
        string.IsNullOrWhiteSpace(term) ? null : term.Trim(),
        status switch
        {
            CatalogStatus.Active => true,
            CatalogStatus.Inactive => false,
            _ => includeInactive ? null : true,
        });
}
