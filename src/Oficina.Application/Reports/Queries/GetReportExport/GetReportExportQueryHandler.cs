using MediatR;
using Oficina.Application.Jobs;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Reports;

namespace Oficina.Application.Reports.Queries.GetReportExport;

public sealed class GetReportExportQueryHandler(IBackgroundJobRepository jobs, ReportCalculator calculator) : IRequestHandler<GetReportExportQuery, Result<ExportJobDto>>
{
    public async Task<Result<ExportJobDto>> Handle(GetReportExportQuery query, CancellationToken cancellationToken) =>
        await jobs.GetByIdAsync(query.Id, cancellationToken) is { Kind: ReportExportRequest.Kind } job ? await calculator.ExportDtoAsync(job, cancellationToken) : ReportErrors.NotFound;
}
