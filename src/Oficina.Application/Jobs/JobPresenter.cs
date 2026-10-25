using System.Text.Json;
using Oficina.Application.Jobs.Dtos;
using Oficina.Application.Jobs.Models;

namespace Oficina.Application.Jobs;

public sealed class JobPresenter(ICustomerInsightQueries insights, TimeProvider timeProvider)
{
    public static readonly JsonSerializerOptions Compact = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions Indented = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task<JobDto> DtoAsync(BackgroundJob job, CancellationToken cancellationToken)
    {
        JobResultDto? result = null;
        if (job.Status == BackgroundJob.Completed && job.Kind == BackgroundJob.CustomerDataExport && job.CustomerId is { } customerId)
        {
            var export = await insights.ExportAsync(customerId, timeProvider.GetUtcNow(), cancellationToken);
            result = new JobResultDto(job.FileName, export is null ? null : JsonSerializer.Serialize(export, Indented), null, null, null);
        }
        else if (job.Status == BackgroundJob.Completed && job.ResultJson is { } json)
        {
            var report = JsonSerializer.Deserialize<ImportReport>(json, Compact)!;
            result = new JobResultDto(null, null, report.TotalLines, report.ValidLines, report.Errors);
        }

        return new JobDto(job.Id, job.Kind, job.Status, job.CreatedAt, job.CompletedAt, result);
    }
}
