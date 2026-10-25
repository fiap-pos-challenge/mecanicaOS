using Oficina.Application.Abstractions.Security;
using Oficina.Application.Dashboard.Dtos;
using Oficina.Application.Settings;

namespace Oficina.Application.Dashboard;

public sealed class DashboardLayouts(SettingsStore settings, ICurrentUser currentUser)
{
    public static readonly IReadOnlyList<string> Cards = ["kpis", "dailyRevenue", "attention", "topServices", "yard"];

    public async Task<IReadOnlyList<DashboardCardDto>> CurrentAsync(CancellationToken cancellationToken) =>
        await settings.GetAsync<List<DashboardCardDto>>(Key(), [], cancellationToken);

    public Task SaveAsync(IReadOnlyList<DashboardCardDto> cards, CancellationToken cancellationToken) => settings.SaveAsync(Key(), cards.ToList(), cancellationToken);

    private string Key() => $"dash:{currentUser.UserId.GetValueOrDefault():N}";
}
