using MediatR;
using Oficina.Application.Portal.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Portal.Queries.GetPortalAccess;

public sealed class GetPortalAccessQueryHandler(PortalAccess access) : IRequestHandler<GetPortalAccessQuery, Result<PortalAccessDto>>
{
    public async Task<Result<PortalAccessDto>> Handle(GetPortalAccessQuery query, CancellationToken cancellationToken)
    {
        var estimate = await access.LinkAsync(query.Token, cancellationToken);
        return estimate.Error is { } error ? error : new PortalAccessDto(estimate.Value.PortalAttemptsLeft);
    }
}
