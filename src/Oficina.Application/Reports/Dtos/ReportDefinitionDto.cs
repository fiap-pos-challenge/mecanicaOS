namespace Oficina.Application.Reports.Dtos;

public sealed record ReportDefinitionDto(string Measure, string GroupBy, ReportPeriodDto Period, string Chart);
