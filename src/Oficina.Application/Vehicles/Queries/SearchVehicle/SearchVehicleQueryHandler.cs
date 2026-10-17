using MediatR;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Application.Vehicles.Queries.SearchVehicle;

public sealed class SearchVehicleQueryHandler(IVehicleQueries queries) : IRequestHandler<SearchVehicleQuery, Result<VehicleDto>>
{
    public async Task<Result<VehicleDto>> Handle(SearchVehicleQuery query, CancellationToken cancellationToken)
    {
        var plate = LicensePlate.Create(query.LicensePlate);
        if (plate.Error is { } error)
        {
            return error;
        }

        if (await queries.FindByPlateAsync(plate.Value, cancellationToken) is not { } vehicle)
        {
            return VehicleErrors.NotFound;
        }

        return vehicle;
    }
}
