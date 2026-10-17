using System.Text.Json;
using System.Text.RegularExpressions;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Analytics;
using Oficina.Application.Dashboard;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Jobs.Entities;
using Oficina.Domain.Jobs.Repositories;
using Oficina.Domain.Reports;

namespace Oficina.Application.Reports;

public sealed partial class ReportService(
    IAnalyticsQueries analytics,
    ISavedReportRepository reports,
    IBackgroundJobRepository jobs,
    IEmailSender emailSender,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    Notifications.INotificationPublisher notifications,
    Abstractions.Realtime.IRealtimePublisher realtime)
{
    public const string ExportKind = "ReportExport";
    public const int MaxRecipients = 10;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly string[] Weekdays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
    private static readonly string[] MonthNames = ["janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"];

    private sealed record ExportRequest(ReportDefinitionDto Definition, string Format);

    public async Task<Result<ReportResultDto>> RunAsync(ReportDefinitionDto? definition, CancellationToken cancellationToken)
    {
        if (Check(definition) is { } error)
        {
            return error;
        }

        return await ComputeAsync(definition!, cancellationToken);
    }

    public async Task<Result<SavedReportListDto>> ListAsync(CancellationToken cancellationToken) =>
        new SavedReportListDto((await reports.ListAsync(cancellationToken)).Select(Dto).ToList());

    public async Task<Result<SavedReportDto>> CreateAsync(string? name, ReportDefinitionDto? definition, CancellationToken cancellationToken)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if ((Check(definition) ?? await NameProblemAsync(trimmed, null, cancellationToken)) is { } error)
        {
            return error;
        }

        var report = SavedReport.Create(trimmed, JsonSerializer.Serialize(definition, Json), timeProvider.GetUtcNow());
        reports.Add(report);
        return await CommitAsync(report);
    }

    public async Task<Result<SavedReportDto>> UpdateAsync(Guid id, string? name, ReportDefinitionDto? definition, CancellationToken cancellationToken)
    {
        if (await reports.GetByIdAsync(id, cancellationToken) is not { } report)
        {
            return ReportErrors.NotFound;
        }

        var trimmed = name?.Trim() ?? string.Empty;
        if ((Check(definition) ?? await NameProblemAsync(trimmed, id, cancellationToken)) is { } error)
        {
            return error;
        }

        report.Update(trimmed, JsonSerializer.Serialize(definition, Json), timeProvider.GetUtcNow());
        return await CommitAsync(report);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await reports.GetByIdAsync(id, cancellationToken) is not { } report)
        {
            return ReportErrors.NotFound;
        }

        reports.Remove(report);
        return await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task<Result<SavedReportDto>> ScheduleAsync(Guid id, ReportScheduleDto? schedule, CancellationToken cancellationToken)
    {
        if (await reports.GetByIdAsync(id, cancellationToken) is not { } report)
        {
            return ReportErrors.NotFound;
        }

        if (schedule is not null)
        {
            var fields = new List<FieldError>();
            if (!Weekdays.Contains(schedule.Weekday))
            {
                fields.Add(new FieldError("weekday", "reports.weekday", "Escolha o dia da semana."));
            }

            if (schedule.Time is null || !TimePattern().IsMatch(schedule.Time))
            {
                fields.Add(new FieldError("time", "reports.time", "Informe o horário no formato HH:mm."));
            }

            var recipients = schedule.Recipients ?? [];
            if (recipients.Count == 0 || recipients.Count > MaxRecipients || recipients.Any(recipient => Email.Create(recipient ?? string.Empty).IsFailure))
            {
                fields.Add(new FieldError("recipients", "reports.recipients", "Informe de 1 a 10 e-mails válidos."));
            }

            if (schedule.Format is not ("xlsx" or "pdf"))
            {
                fields.Add(new FieldError("format", "reports.format", "Escolha Excel (xlsx) ou PDF."));
            }

            if (fields.Count > 0)
            {
                return new Error("reports.schedule", "Revise o agendamento.", ErrorType.Validation, fields);
            }

            schedule = schedule with { Recipients = recipients.Select(recipient => recipient.Trim().ToLowerInvariant()).Distinct(StringComparer.Ordinal).ToList() };
        }

        report.Schedule(schedule is null ? null : JsonSerializer.Serialize(schedule, Json), timeProvider.GetUtcNow());
        return await CommitAsync(report);
    }

    public async Task<Result<ExportJobDto>> ExportAsync(ReportDefinitionDto? definition, string? format, CancellationToken cancellationToken)
    {
        if (Check(definition) is { } error)
        {
            return error;
        }

        if (format is not ("xlsx" or "pdf"))
        {
            return ReportErrors.InvalidFormat;
        }

        var now = timeProvider.GetUtcNow();
        var job = BackgroundJob.Queue(ExportKind, currentUser.UserId ?? Guid.Empty, null, now);
        job.Complete($"relatorio.{format}", JsonSerializer.Serialize(new ExportRequest(definition!, format), Json), now);
        jobs.Add(job);
        await notifications.PublishAsync("ExportReady", "Exportação de relatório pronta", $"{ReportEngine.Explain(definition!)} ({format}).", "/reports", false, null, cancellationToken);
        await realtime.PublishAsync(new { type = "exportReady", exportId = job.Id, occurredAt = now }, cancellationToken);
        var committed = await unitOfWork.CommitAsync(cancellationToken);
        return committed.Error is { } commitError ? commitError : await ExportDtoAsync(job, cancellationToken);
    }

    public async Task<Result<ExportJobDto>> GetExportAsync(Guid id, CancellationToken cancellationToken) =>
        await jobs.GetByIdAsync(id, cancellationToken) is { Kind: ExportKind } job ? await ExportDtoAsync(job, cancellationToken) : ReportErrors.NotFound;

    public async Task<int> SendDueAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var local = now.ToOffset(DashboardService.Brazil);
        var sent = 0;
        foreach (var report in await reports.ListAsync(cancellationToken))
        {
            if (report.ScheduleJson is null || JsonSerializer.Deserialize<ReportScheduleDto>(report.ScheduleJson, Json) is not { } schedule
                || schedule.Weekday != local.DayOfWeek.ToString() || string.CompareOrdinal(schedule.Time, local.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture)) > 0
                || (report.LastSentAt is { } last && last.ToOffset(DashboardService.Brazil).Date == local.Date))
            {
                continue;
            }

            var definition = JsonSerializer.Deserialize<ReportDefinitionDto>(report.DefinitionJson, Json)!;
            var result = await ComputeAsync(definition, cancellationToken);
            foreach (var recipient in schedule.Recipients)
            {
                await emailSender.SendAsync(new EmailMessage(recipient, $"Relatório agendado: {report.Name}", ReportFiles.Html(report.Name, result)), cancellationToken);
            }

            report.MarkSent(now);
            sent++;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return sent;
    }

    public Result<ReportSuggestionDto> Suggest(string? question)
    {
        var text = (question ?? string.Empty).Trim();
        if (text.Length is < 8 or > 300)
        {
            return ReportErrors.InvalidQuestion;
        }

        var lower = text.ToLowerInvariant();
        string? measure = lower switch
        {
            _ when lower.Contains("ticket", StringComparison.Ordinal) => "averageTicket",
            _ when lower.Contains("margem", StringComparison.Ordinal) => "margin",
            _ when lower.Contains("comiss", StringComparison.Ordinal) => "commission",
            _ when lower.Contains("peça", StringComparison.Ordinal) || lower.Contains("peca", StringComparison.Ordinal) => "partsOut",
            _ when QuantityPattern().IsMatch(lower) => "orderCount",
            _ when RevenuePattern().IsMatch(lower) => "revenue",
            _ => null,
        };
        if (measure is null)
        {
            return ReportErrors.NotUnderstood;
        }

        string wanted = lower switch
        {
            _ when MechanicPattern().IsMatch(lower) => "mechanic",
            _ when lower.Contains("pagamento", StringComparison.Ordinal) => "paymentMethod",
            _ when lower.Contains("categoria", StringComparison.Ordinal) => "category",
            _ when lower.Contains("serviço", StringComparison.Ordinal) || lower.Contains("servico", StringComparison.Ordinal) => "service",
            _ when lower.Contains("cliente", StringComparison.Ordinal) => "customer",
            _ when lower.Contains("semana", StringComparison.Ordinal) => "week",
            _ => "month",
        };
        var groupBy = ReportEngine.AllowedGroupings(measure).Contains(wanted) ? wanted : "month";
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().ToOffset(DashboardService.Brazil).DateTime);
        var monthIndex = Array.FindIndex(MonthNames, name => lower.Contains(name, StringComparison.Ordinal));
        var period = monthIndex >= 0
            ? new ReportPeriodDto("custom", ReportEngine.Iso(new DateOnly(today.Year, monthIndex + 1, 1)), ReportEngine.Iso(new DateOnly(today.Year, monthIndex + 1, 1).AddMonths(1).AddDays(-1)))
            : lower.Contains("mês passado", StringComparison.Ordinal) || lower.Contains("mes passado", StringComparison.Ordinal) ? new ReportPeriodDto("lastMonth", null, null)
            : lower.Contains("trimestre", StringComparison.Ordinal) ? new ReportPeriodDto("lastQuarter", null, null)
            : lower.Contains("este mês", StringComparison.Ordinal) || lower.Contains("esse mês", StringComparison.Ordinal) || lower.Contains("este mes", StringComparison.Ordinal) ? new ReportPeriodDto("thisMonth", null, null)
            : new ReportPeriodDto("yearToDate", null, null);
        var overTime = groupBy is "month" or "week";
        var additive = measure is not ("averageTicket" or "margin");
        var chart = overTime ? "line" : additive && SharePattern().IsMatch(lower) ? "donut" : "column";
        var definition = new ReportDefinitionDto(measure, groupBy, period, chart);
        return new ReportSuggestionDto(definition, ReportEngine.Explain(definition));
    }

    [GeneratedRegex("^([01][0-9]|2[0-3]):[0-5][0-9]$")]
    private static partial Regex TimePattern();

    [GeneratedRegex("quant|quantas|número de os|numero de os")]
    private static partial Regex QuantityPattern();

    [GeneratedRegex("fatur|vend|receita")]
    private static partial Regex RevenuePattern();

    [GeneratedRegex("mecânic|mecanic")]
    private static partial Regex MechanicPattern();

    [GeneratedRegex("parte|divid|particip")]
    private static partial Regex SharePattern();

    private static Error? Check(ReportDefinitionDto? definition) =>
        !ReportEngine.IsReadable(definition) ? ReportErrors.InvalidDefinition
        : ReportEngine.Problem(definition!) is { } problem ? ReportErrors.Incompatible(problem)
        : null;

    private static SavedReportDto Dto(SavedReport report) => new(
        report.Id,
        report.Name,
        JsonSerializer.Deserialize<ReportDefinitionDto>(report.DefinitionJson, Json)!,
        report.ScheduleJson is null ? null : JsonSerializer.Deserialize<ReportScheduleDto>(report.ScheduleJson, Json),
        report.UpdatedAt);

    private async Task<Error?> NameProblemAsync(string name, Guid? id, CancellationToken cancellationToken)
    {
        if (name.Length is < 3 or > SavedReport.NameMaxLength)
        {
            return ReportErrors.InvalidName;
        }

        return await reports.NameExistsAsync(name, id, cancellationToken) ? ReportErrors.DuplicateName : null;
    }

    private async Task<ReportResultDto> ComputeAsync(ReportDefinitionDto definition, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().ToOffset(DashboardService.Brazil).DateTime);
        var (from, to) = ReportEngine.Range(definition.Period, today);
        var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), DashboardService.Brazil);
        var end = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), DashboardService.Brazil);
        var sales = await analytics.SalesAsync(start, end, cancellationToken);
        return ReportEngine.Run(definition, sales.Where(sale => sale.DeliveredAt < end).ToList(), from, to, DashboardService.Brazil);
    }

    private async Task<ExportJobDto> ExportDtoAsync(BackgroundJob job, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<ExportRequest>(job.ResultJson!, Json)!;
        var result = await ComputeAsync(request.Definition, cancellationToken);
        var title = ReportEngine.Explain(request.Definition);
        var url = request.Format == "xlsx"
            ? $"data:{ReportFiles.XlsxType};base64,{Convert.ToBase64String(ReportFiles.Xlsx(title, result))}"
            : $"data:application/pdf;base64,{Convert.ToBase64String(ReportFiles.Pdf(title, result))}";
        return new ExportJobDto(job.Id, request.Format, "Ready", false, url, job.FileName);
    }

    private async Task<Result<SavedReportDto>> CommitAsync(SavedReport report) =>
        (await unitOfWork.CommitAsync(CancellationToken.None)).Error is { } error ? error : Dto(report);
}
