using MediatR;
using Oficina.Application.Dashboard;
using Oficina.Application.Finance.Dtos;
using Oficina.Application.Settings;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Finance.Queries.GetPlatform;

public sealed class GetPlatformQueryHandler(IFinanceQueries queries, SettingsStore settings, TimeProvider timeProvider) : IRequestHandler<GetPlatformQuery, Result<PlatformDto>>
{
    public static readonly IReadOnlyList<PlatformModuleDto> Modules =
    [
        new("serviceOrders", "Ordens de serviço", true),
        new("quotes", "Orçamentos e aprovação", true),
        new("portal", "Portal do cliente", true),
        new("inventory", "Estoque e compras", true),
        new("reports", "Relatórios e assistente", true),
        new("automations", "Automações", true),
        new("finance", "Financeiro", true),
        new("fiscal", "Notas fiscais", false),
    ];

    public async Task<Result<PlatformDto>> Handle(GetPlatformQuery query, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var workshop = await settings.GetAsync(SettingsKeys.Workshop, SettingsDefaults.Workshop(now), cancellationToken);
        var local = now.ToOffset(DashboardClock.Brazil);
        var monthStart = new DateTimeOffset(local.Year, local.Month, 1, 0, 0, 0, DashboardClock.Brazil);
        var users = await queries.ActiveUsersAsync(cancellationToken);
        return new PlatformDto(workshop.Name, new PlatformPlanDto(workshop.Plan.Name, workshop.Plan.RenewsAt, users, workshop.Plan.MaxUsers), await queries.UsageAsync(monthStart, cancellationToken), Modules);
    }
}
