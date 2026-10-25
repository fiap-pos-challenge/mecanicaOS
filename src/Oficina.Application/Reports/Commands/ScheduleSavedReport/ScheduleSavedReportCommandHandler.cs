using System.Text.Json;
using System.Text.RegularExpressions;
using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Reports;

namespace Oficina.Application.Reports.Commands.ScheduleSavedReport;

public sealed partial class ScheduleSavedReportCommandHandler(ISavedReportRepository reports, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IRequestHandler<ScheduleSavedReportCommand, Result<SavedReportDto>>
{
    public const int MaxRecipients = 10;
    private static readonly string[] Weekdays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

    public async Task<Result<SavedReportDto>> Handle(ScheduleSavedReportCommand command, CancellationToken cancellationToken)
    {
        if (await reports.GetByIdAsync(command.Id, cancellationToken) is not { } report)
        {
            return ReportErrors.NotFound;
        }

        var schedule = command.Schedule;
        if (schedule is not null)
        {
            var fields = new List<FieldError>();
            if (!Weekdays.Contains(schedule.Weekday))
            {
                fields.Add(new FieldError("weekday", "reports.weekday", ReportTexts.Weekday));
            }

            if (schedule.Time is null || !TimePattern().IsMatch(schedule.Time))
            {
                fields.Add(new FieldError("time", "reports.time", ReportTexts.Time));
            }

            var recipients = schedule.Recipients ?? [];
            if (recipients.Count == 0 || recipients.Count > MaxRecipients || recipients.Any(recipient => Email.Create(recipient ?? string.Empty).IsFailure))
            {
                fields.Add(new FieldError("recipients", "reports.recipients", ReportTexts.Recipients));
            }

            if (!ReportExportRequest.IsFormat(schedule.Format))
            {
                fields.Add(new FieldError("format", "reports.format", ReportTexts.Format));
            }

            if (fields.Count > 0)
            {
                return new Error("reports.schedule", ReportTexts.ScheduleInvalid, ErrorType.Validation, fields);
            }

            schedule = schedule with { Recipients = recipients.Select(recipient => recipient.Trim().ToLowerInvariant()).Distinct(StringComparer.Ordinal).ToList() };
        }

        report.Schedule(schedule is null ? null : JsonSerializer.Serialize(schedule, ReportDefinitions.Json), timeProvider.GetUtcNow());
        return (await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError ? commitError : report.ToDto();
    }

    [GeneratedRegex("^([01][0-9]|2[0-3]):[0-5][0-9]$")]
    private static partial Regex TimePattern();
}
