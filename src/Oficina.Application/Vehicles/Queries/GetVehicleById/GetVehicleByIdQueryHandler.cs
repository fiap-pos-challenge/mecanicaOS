using MediatR;
using Oficina.Application.Vehicles.Dto;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;

namespace Oficina.Application.Vehicles.Queries.GetById;

public sealed class GetVehicleByIdQueryHandler(IVehiclesQueries queries) : IRequestHandler<GetVehicleByIdQuery, Result<VehicleDto>>
{
    public async Task<Result<VehicleDto>> Handle(GetVehicleByIdQuery query, CancellationToken cancellationToken)
    {
        if (await queries.GetByIdAsync(query.Id, cancellationToken) is not { } vehicle)
        {
            return VehicleErrors.NotFound;
        }

        return vehicle;
    }
}
