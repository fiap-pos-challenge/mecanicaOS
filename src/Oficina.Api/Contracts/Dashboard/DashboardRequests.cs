using Oficina.Application.Dashboard;

namespace Oficina.Api.Contracts.Dashboard;

public sealed record DashboardLayoutBody(IReadOnlyList<DashboardCardDto>? Cards);

public sealed record DashboardActionBody(string? Action);
