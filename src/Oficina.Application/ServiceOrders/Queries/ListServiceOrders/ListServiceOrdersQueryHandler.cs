using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Validation;
using Oficina.Application.Vehicles.Mappers;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Queries.ListServiceOrders;

public sealed class ListServiceOrdersQueryHandler(IServiceOrderQueries queries)
    : IRequestHandler<ListServiceOrdersQuery, Result<PagedResult<ServiceOrderSummaryDto>>>
{
    public async Task<Result<PagedResult<ServiceOrderSummaryDto>>> Handle(ListServiceOrdersQuery query, CancellationToken cancellationToken)
    {
        var status = ServiceOrderRules.ToOptionalStatus(query.Status);
        if (status.Error is { } statusError)
        {
            return statusError;
        }

        var plate = VehicleMapper.ToOptionalLicensePlate(query.Plate);
        if (plate.Error is { } plateError)
        {
            return plateError;
        }

        return await queries.ListAsync(new PageRequest(query.Page, query.PageSize), status.Value, plate.Value, cancellationToken);
    }
}
