using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Common.Pagination;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Queries.ListParts;

public sealed class ListPartsQueryHandler(IPartQueries queries)
    : IRequestHandler<ListPartsQuery, Result<PagedResult<PartDto>>>
{
    public async Task<Result<PagedResult<PartDto>>> Handle(ListPartsQuery query, CancellationToken cancellationToken)
    {
        return await queries.ListAsync(new PageRequest(query.Page, query.PageSize), CatalogFilter.From(query.Term, query.Status, query.IncludeInactive), query.IncludeCost, cancellationToken);
    }
}
