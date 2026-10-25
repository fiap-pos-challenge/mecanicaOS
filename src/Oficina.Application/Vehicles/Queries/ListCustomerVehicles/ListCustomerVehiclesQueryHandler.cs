using MediatR;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;

namespace Oficina.Application.Vehicles.Queries.ListCustomerVehicles;

public sealed class ListCustomerVehiclesQueryHandler(ICustomerRepository customers, IVehicleQueries queries)
    : IRequestHandler<ListCustomerVehiclesQuery, Result<VehicleListDto>>
{
    public async Task<Result<VehicleListDto>> Handle(ListCustomerVehiclesQuery query, CancellationToken cancellationToken)
    {
        if (await customers.GetByIdAsync(query.CustomerId, cancellationToken) is null)
        {
            return CustomerErrors.NotFound;
        }

        return new VehicleListDto(await queries.ListByCustomerAsync(query.CustomerId, cancellationToken));
    }
}
