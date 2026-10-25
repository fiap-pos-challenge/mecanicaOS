namespace Oficina.Application.Reports.Dtos;

public sealed record ReportScheduleDto(string Weekday, string Time, IReadOnlyList<string> Recipients, string Format);
