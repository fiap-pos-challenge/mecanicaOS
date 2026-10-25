using MediatR;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Reports.Commands.ExportReport;

public sealed record ExportReportCommand(ReportDefinitionDto? Definition, string? Format) : IRequest<Result<ExportJobDto>>;
