using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Portal.Queries.GetPortalSessionOrder;

public sealed class GetPortalSessionOrderQueryHandler(PortalAccess access) : IRequestHandler<GetPortalSessionOrderQuery, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(GetPortalSessionOrderQuery query, CancellationToken cancellationToken)
    {
        var estimate = await access.SessionAsync(query.Session, cancellationToken);
        return estimate.Error is { } error ? error : estimate.Value.ServiceOrderId;
    }
}
