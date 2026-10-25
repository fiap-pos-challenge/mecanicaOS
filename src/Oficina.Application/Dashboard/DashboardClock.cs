namespace Oficina.Application.Dashboard;

public static class DashboardClock
{
    public static readonly TimeSpan Brazil = TimeSpan.FromHours(-3);

    public static DateTimeOffset Today(DateTimeOffset now) => new(now.ToOffset(Brazil).Date, Brazil);

    public static int? PeriodDays(string? period) => period switch { null or "" or "today" => 1, "week" => 7, "month" => 30, _ => null };
}
