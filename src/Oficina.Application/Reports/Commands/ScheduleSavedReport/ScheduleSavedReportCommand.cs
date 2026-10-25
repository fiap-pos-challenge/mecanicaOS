using MediatR;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Reports.Commands.ScheduleSavedReport;

public sealed record ScheduleSavedReportCommand(Guid Id, ReportScheduleDto? Schedule) : IRequest<Result<SavedReportDto>>;
