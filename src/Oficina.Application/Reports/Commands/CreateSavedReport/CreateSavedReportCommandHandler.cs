using System.Text.Json;
using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Reports;

namespace Oficina.Application.Reports.Commands.CreateSavedReport;

public sealed class CreateSavedReportCommandHandler(ISavedReportRepository reports, IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<CreateSavedReportCommand, Result<SavedReportDto>>
{
    public async Task<Result<SavedReportDto>> Handle(CreateSavedReportCommand command, CancellationToken cancellationToken)
    {
        var name = command.Name?.Trim() ?? string.Empty;
        if ((ReportDefinitions.Check(command.Definition) ?? await ReportDefinitions.NameProblemAsync(reports, name, null, cancellationToken)) is { } error)
        {
            return error;
        }

        var report = SavedReport.Create(name, JsonSerializer.Serialize(command.Definition, ReportDefinitions.Json), timeProvider.GetUtcNow());
        reports.Add(report);
        return (await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError ? commitError : report.ToDto();
    }
}
