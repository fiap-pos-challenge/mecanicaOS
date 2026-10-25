using Oficina.Application.Reports.Dtos;

namespace Oficina.Api.Contracts.Reports;

public sealed record ExportReportBody(ReportDefinitionDto? Definition, string? Format);
