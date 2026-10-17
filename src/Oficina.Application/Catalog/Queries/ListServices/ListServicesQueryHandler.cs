using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Common.Pagination;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Queries.ListServices;

public sealed class ListServicesQueryHandler(IServiceQueries queries)
    : IRequestHandler<ListServicesQuery, Result<PagedResult<ServiceDto>>>
{
    public async Task<Result<PagedResult<ServiceDto>>> Handle(ListServicesQuery query, CancellationToken cancellationToken) =>
        await queries.ListAsync(
            new PageRequest(query.Page, query.PageSize),
            CatalogFilter.From(query.Term, query.Status, query.IncludeInactive),
            cancellationToken);
}
