using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Dashboard;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Reports;

namespace Oficina.Application.Reports;

public sealed partial class ScheduledReportSender(
    ISavedReportRepository reports,
    ReportCalculator calculator,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ScheduledReportSender> logger)
{
    public async Task<int> SendDueAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var local = now.ToOffset(DashboardClock.Brazil);
        var sent = 0;
        foreach (var report in await reports.ListAsync(cancellationToken))
        {
            if (report.ScheduleJson is null || JsonSerializer.Deserialize<ReportScheduleDto>(report.ScheduleJson, ReportDefinitions.Json) is not { } schedule
                || schedule.Weekday != local.DayOfWeek.ToString() || string.CompareOrdinal(schedule.Time, local.ToString("HH:mm", CultureInfo.InvariantCulture)) > 0
                || (report.LastSentAt is { } last && last.ToOffset(DashboardClock.Brazil).Date == local.Date))
            {
                continue;
            }

            var definition = JsonSerializer.Deserialize<ReportDefinitionDto>(report.DefinitionJson, ReportDefinitions.Json)!;
            var result = await calculator.ComputeAsync(definition, cancellationToken);
            report.MarkSent(now);
            if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } error)
            {
                LogCommitFailed(logger, error.Code);
                return sent;
            }

            foreach (var recipient in schedule.Recipients)
            {
                await emailSender.SendAsync(new EmailMessage(recipient, ReportTexts.ScheduledSubject(report.Name), ReportFiles.Html(report.Name, result)), cancellationToken);
            }

            sent++;
        }

        return sent;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Scheduled report commit failed: {ErrorCode}")]
    private static partial void LogCommitFailed(ILogger logger, string errorCode);
}
