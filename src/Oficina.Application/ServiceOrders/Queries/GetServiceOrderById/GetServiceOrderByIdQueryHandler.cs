using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.Application.ServiceOrders.Queries.GetServiceOrderById;

public sealed class GetServiceOrderByIdQueryHandler(IServiceOrderQueries queries)
    : IRequestHandler<GetServiceOrderByIdQuery, Result<ServiceOrderDetailDto>>
{
    public async Task<Result<ServiceOrderDetailDto>> Handle(GetServiceOrderByIdQuery query, CancellationToken cancellationToken)
    {
        if (await queries.GetByIdAsync(query.Id, cancellationToken) is not { } serviceOrder)
        {
            return ServiceOrderErrors.NotFound;
        }

        return serviceOrder;
    }
}
