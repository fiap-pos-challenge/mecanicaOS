using MediatR;
using Oficina.Application.ServiceOrders.Commands;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Queries.GetWork;

public sealed class GetWorkQueryHandler(IServiceOrderBoardQueries queries) : IRequestHandler<GetWorkQuery, Result<MechanicOrderDto>>
{
    public Task<Result<MechanicOrderDto>> Handle(GetWorkQuery query, CancellationToken cancellationToken) =>
        ServiceOrderResponses.WorkAsync(queries, query.Id, cancellationToken);
}
