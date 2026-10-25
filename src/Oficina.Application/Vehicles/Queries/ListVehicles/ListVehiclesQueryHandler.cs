using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Vehicles.Queries.ListVehicles;

public sealed class ListVehiclesQueryHandler(IVehicleQueries queries) : IRequestHandler<ListVehiclesQuery, Result<PagedResult<VehicleDto>>>
{
    public async Task<Result<PagedResult<VehicleDto>>> Handle(ListVehiclesQuery query, CancellationToken cancellationToken) =>
        await queries.ListAsync(new PageRequest(query.Page, query.PageSize), cancellationToken);
}
