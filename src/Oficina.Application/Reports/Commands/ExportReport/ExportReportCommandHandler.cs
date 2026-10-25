using System.Text.Json;
using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Realtime;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Jobs;
using Oficina.Application.Jobs.Models;
using Oficina.Application.Notifications;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Reports;

namespace Oficina.Application.Reports.Commands.ExportReport;

public sealed class ExportReportCommandHandler(
    IBackgroundJobRepository jobs,
    ReportCalculator calculator,
    ITeamNotificationPublisher notifications,
    IRealtimePublisher realtime,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<ExportReportCommand, Result<ExportJobDto>>
{
    public async Task<Result<ExportJobDto>> Handle(ExportReportCommand command, CancellationToken cancellationToken)
    {
        if (ReportDefinitions.Check(command.Definition) is { } error)
        {
            return error;
        }

        if (!ReportExportRequest.IsFormat(command.Format))
        {
            return ReportErrors.InvalidFormat;
        }

        var definition = command.Definition!;
        var format = command.Format!;
        var now = timeProvider.GetUtcNow();
        var job = BackgroundJob.Queue(ReportExportRequest.Kind, currentUser.UserId ?? Guid.Empty, null, now);
        job.Complete(ReportTexts.FileName(format), JsonSerializer.Serialize(new ReportExportRequest(definition, format), ReportDefinitions.Json), now);
        jobs.Add(job);
        await notifications.PublishAsync(NotificationTypes.ExportReady, ReportTexts.ExportReadyTitle, ReportTexts.ExportReadyBody(ReportEngine.Explain(definition), format), "/reports", false, null, cancellationToken);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        await realtime.PublishAsync(new { type = "exportReady", exportId = job.Id, occurredAt = now }, cancellationToken);
        return await calculator.ExportDtoAsync(job, cancellationToken);
    }
}
