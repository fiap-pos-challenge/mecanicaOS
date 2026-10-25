using Oficina.Application.Dashboard.Dtos;

namespace Oficina.Api.Contracts.Dashboard;

public sealed record DashboardLayoutBody(IReadOnlyList<DashboardCardDto>? Cards);
