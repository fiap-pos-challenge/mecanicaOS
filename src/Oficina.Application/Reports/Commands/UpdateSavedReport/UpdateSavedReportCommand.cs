using MediatR;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Reports.Commands.UpdateSavedReport;

public sealed record UpdateSavedReportCommand(Guid Id, string? Name, ReportDefinitionDto? Definition) : IRequest<Result<SavedReportDto>>;
