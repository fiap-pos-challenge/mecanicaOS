namespace Oficina.Application.Dashboard.Dtos;

public sealed record DashboardSnapshotDto(
    IReadOnlyList<DashboardKpiDto> Kpis,
    IReadOnlyList<DailyRevenueDto> DailyRevenue,
    IReadOnlyList<TopServiceDto> TopServices,
    IReadOnlyList<YardCountDto> Yard,
    IReadOnlyList<AttentionItemDto> Attention);
