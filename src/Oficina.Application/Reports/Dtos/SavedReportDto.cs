namespace Oficina.Application.Reports.Dtos;

public sealed record SavedReportDto(Guid Id, string Name, ReportDefinitionDto Definition, ReportScheduleDto? Schedule, DateTimeOffset UpdatedAt);
