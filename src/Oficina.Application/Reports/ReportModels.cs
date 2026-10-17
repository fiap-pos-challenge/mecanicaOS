namespace Oficina.Application.Reports;

public sealed record ReportPeriodDto(string Preset, string? From, string? To);

public sealed record ReportDefinitionDto(string Measure, string GroupBy, ReportPeriodDto Period, string Chart);

public sealed record ReportRowDto(string Label, decimal Value);

public sealed record ReportResultDto(IReadOnlyList<ReportRowDto> Rows, string Unit, decimal? Total, string From, string To);

public sealed record ReportScheduleDto(string Weekday, string Time, IReadOnlyList<string> Recipients, string Format);

public sealed record SavedReportDto(Guid Id, string Name, ReportDefinitionDto Definition, ReportScheduleDto? Schedule, DateTimeOffset UpdatedAt);

public sealed record SavedReportListDto(IReadOnlyList<SavedReportDto> Items);

public sealed record ExportJobDto(Guid Id, string Format, string Status, bool Large, string? FileUrl, string? FileName);

public sealed record ReportSuggestionDto(ReportDefinitionDto Definition, string Explanation);
