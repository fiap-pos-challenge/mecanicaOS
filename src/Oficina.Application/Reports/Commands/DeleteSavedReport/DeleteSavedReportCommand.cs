using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Reports.Commands.DeleteSavedReport;

public sealed record DeleteSavedReportCommand(Guid Id) : IRequest<Result>;
