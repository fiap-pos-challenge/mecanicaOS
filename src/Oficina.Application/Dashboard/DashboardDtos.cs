namespace Oficina.Application.Dashboard;

public sealed record DashboardKpiDto(string Id, long Value, long? Previous, long? Detail);

public sealed record DailyRevenueDto(string Date, long Services, long Parts);

public sealed record TopServiceDto(string Name, int Quantity);

public sealed record YardCountDto(string Status, int Count);

public sealed record AttentionItemDto(string Id, string Kind, string Severity, string Title, string Detail, string? Action, IReadOnlyDictionary<string, string>? Target, bool AiSuggested);

public sealed record DashboardSnapshotDto(
    IReadOnlyList<DashboardKpiDto> Kpis,
    IReadOnlyList<DailyRevenueDto> DailyRevenue,
    IReadOnlyList<TopServiceDto> TopServices,
    IReadOnlyList<YardCountDto> Yard,
    IReadOnlyList<AttentionItemDto> Attention);

public sealed record DashboardCardDto(string Id, bool Visible);

public sealed record DashboardLayoutDto(IReadOnlyList<DashboardCardDto> Cards);
