using Oficina.Application.Reports;

namespace Oficina.Api.Contracts.Reports;

public sealed record RunReportBody(ReportDefinitionDto? Definition);

public sealed record SaveReportBody(string? Name, ReportDefinitionDto? Definition);

public sealed record ScheduleReportBody(ReportScheduleDto? Schedule);

public sealed record ExportReportBody(ReportDefinitionDto? Definition, string? Format);

public sealed record AssistantQuestionBody(string? Question);
