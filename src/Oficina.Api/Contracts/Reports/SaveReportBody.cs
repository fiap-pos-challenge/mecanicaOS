using Oficina.Application.Reports.Dtos;

namespace Oficina.Api.Contracts.Reports;

public sealed record SaveReportBody(string? Name, ReportDefinitionDto? Definition);
