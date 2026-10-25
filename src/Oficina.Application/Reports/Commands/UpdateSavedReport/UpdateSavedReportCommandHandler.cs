using System.Text.Json;
using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Reports;

namespace Oficina.Application.Reports.Commands.UpdateSavedReport;

public sealed class UpdateSavedReportCommandHandler(ISavedReportRepository reports, IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<UpdateSavedReportCommand, Result<SavedReportDto>>
{
    public async Task<Result<SavedReportDto>> Handle(UpdateSavedReportCommand command, CancellationToken cancellationToken)
    {
        if (await reports.GetByIdAsync(command.Id, cancellationToken) is not { } report)
        {
            return ReportErrors.NotFound;
        }

        var name = command.Name?.Trim() ?? string.Empty;
        if ((ReportDefinitions.Check(command.Definition) ?? await ReportDefinitions.NameProblemAsync(reports, name, command.Id, cancellationToken)) is { } error)
        {
            return error;
        }

        report.Update(name, JsonSerializer.Serialize(command.Definition, ReportDefinitions.Json), timeProvider.GetUtcNow());
        return (await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError ? commitError : report.ToDto();
    }
}
