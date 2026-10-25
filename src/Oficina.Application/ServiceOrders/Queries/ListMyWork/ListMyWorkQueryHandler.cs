using MediatR;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Queries.ListMyWork;

public sealed class ListMyWorkQueryHandler(IServiceOrderBoardQueries queries, ICurrentUser currentUser)
    : IRequestHandler<ListMyWorkQuery, Result<MechanicOrderListDto>>
{
    public async Task<Result<MechanicOrderListDto>> Handle(ListMyWorkQuery query, CancellationToken cancellationToken) =>
        currentUser.UserId is { } userId
            ? new MechanicOrderListDto(await queries.ListMineAsync(userId, cancellationToken))
            : CommonErrors.Unauthorized;
}
