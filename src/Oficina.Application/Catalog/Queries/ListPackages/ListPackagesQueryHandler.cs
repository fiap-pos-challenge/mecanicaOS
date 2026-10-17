using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Common.Pagination;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Queries.ListPackages;

public sealed class ListPackagesQueryHandler(IPackageQueries queries)
    : IRequestHandler<ListPackagesQuery, Result<PagedResult<PackageDto>>>
{
    public async Task<Result<PagedResult<PackageDto>>> Handle(ListPackagesQuery query, CancellationToken cancellationToken)
    {
        return await queries.ListAsync(new PageRequest(query.Page, query.PageSize), CatalogFilter.From(query.Term, query.Status, query.IncludeInactive), cancellationToken);
    }
}
