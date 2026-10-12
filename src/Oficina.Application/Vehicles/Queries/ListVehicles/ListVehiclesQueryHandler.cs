using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Application.Vehicles.Queries.ListVehicles;

public sealed class ListVehiclesQueryHandler(IVehicleQueries queries) : IRequestHandler<ListVehiclesQuery, Result<PagedResult<VehicleDto>>>
{
    public async Task<Result<PagedResult<VehicleDto>>> Handle(ListVehiclesQuery query, CancellationToken cancellationToken)
    {
        LicensePlate? plate = null;
        if (!string.IsNullOrWhiteSpace(query.Plate))
        {
            var result = LicensePlate.Create(query.Plate);
            if (result.Error is { } error)
            {
                return error;
            }

            plate = result.Value;
        }

        return await queries.ListAsync(new PageRequest(query.Page, query.PageSize), plate, cancellationToken);
    }
}
