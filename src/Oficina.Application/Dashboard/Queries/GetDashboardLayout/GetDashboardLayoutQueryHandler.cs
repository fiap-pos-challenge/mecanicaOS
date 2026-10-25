using MediatR;
using Oficina.Application.Dashboard.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Dashboard.Queries.GetDashboardLayout;

public sealed class GetDashboardLayoutQueryHandler(DashboardLayouts layouts) : IRequestHandler<GetDashboardLayoutQuery, Result<DashboardLayoutDto>>
{
    public async Task<Result<DashboardLayoutDto>> Handle(GetDashboardLayoutQuery query, CancellationToken cancellationToken) =>
        new DashboardLayoutDto(await layouts.CurrentAsync(cancellationToken));
}
