using MediatR;
using Oficina.Application.Portal.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Portal.Queries.GetPortalOrder;

public sealed class GetPortalOrderQueryHandler(PortalAccess access, PortalOrderView view) : IRequestHandler<GetPortalOrderQuery, Result<PortalOrderDto>>
{
    public async Task<Result<PortalOrderDto>> Handle(GetPortalOrderQuery query, CancellationToken cancellationToken)
    {
        var estimate = await access.SessionAsync(query.Session, cancellationToken);
        return estimate.Error is { } error ? error : await view.ViewAsync(estimate.Value, cancellationToken);
    }
}
