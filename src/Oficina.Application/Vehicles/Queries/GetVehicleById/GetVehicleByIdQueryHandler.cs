using MediatR;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;

namespace Oficina.Application.Vehicles.Queries.GetVehicleById;

public sealed class GetVehicleByIdQueryHandler(IVehicleQueries queries) : IRequestHandler<GetVehicleByIdQuery, Result<VehicleDetailDto>>
{
    public async Task<Result<VehicleDetailDto>> Handle(GetVehicleByIdQuery query, CancellationToken cancellationToken)
    {
        if (await queries.GetByIdAsync(query.Id, cancellationToken) is not { } vehicle)
        {
            return VehicleErrors.NotFound;
        }

        return vehicle;
    }
}
