using System.Text.Json;
using Oficina.Application.Analytics;
using Oficina.Application.Dashboard;
using Oficina.Application.Jobs.Models;
using Oficina.Application.Reports.Dtos;

namespace Oficina.Application.Reports;

public sealed class ReportCalculator(IAnalyticsQueries analytics, TimeProvider timeProvider)
{
    public const string Ready = "Ready";

    public async Task<ReportResultDto> ComputeAsync(ReportDefinitionDto definition, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().ToOffset(DashboardClock.Brazil).DateTime);
        var (from, to) = ReportEngine.Range(definition.Period, today);
        var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), DashboardClock.Brazil);
        var end = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), DashboardClock.Brazil);
        var sales = await analytics.SalesAsync(start, end, cancellationToken);
        return ReportEngine.Run(definition, sales.Where(sale => sale.DeliveredAt < end).ToList(), from, to, DashboardClock.Brazil);
    }

    public async Task<ExportJobDto> ExportDtoAsync(BackgroundJob job, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<ReportExportRequest>(job.ResultJson!, ReportDefinitions.Json)!;
        var result = await ComputeAsync(request.Definition, cancellationToken);
        var title = ReportEngine.Explain(request.Definition);
        var url = request.Format == ReportExportRequest.Xlsx
            ? $"data:{ReportFiles.XlsxType};base64,{Convert.ToBase64String(ReportFiles.Xlsx(title, result))}"
            : $"data:application/pdf;base64,{Convert.ToBase64String(ReportFiles.Pdf(title, result))}";
        return new ExportJobDto(job.Id, request.Format, Ready, false, url, job.FileName);
    }
}
