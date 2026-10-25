using MediatR;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Reports.Commands.CreateSavedReport;

public sealed record CreateSavedReportCommand(string? Name, ReportDefinitionDto? Definition) : IRequest<Result<SavedReportDto>>;
