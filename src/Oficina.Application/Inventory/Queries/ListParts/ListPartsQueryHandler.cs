using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Queries.ListParts;

public sealed class ListPartsQueryHandler(IPartQueries queries)
    : IRequestHandler<ListPartsQuery, Result<PagedResult<PartDto>>>
{
    public async Task<Result<PagedResult<PartDto>>> Handle(ListPartsQuery query, CancellationToken cancellationToken) =>
        await queries.ListAsync(new PageRequest(query.Page, query.PageSize), query.IncludeInactive, query.BelowMinimumOnly, cancellationToken);
}
