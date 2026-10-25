using MediatR;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Reports.Queries.RunReport;

public sealed class RunReportQueryHandler(ReportCalculator calculator) : IRequestHandler<RunReportQuery, Result<ReportResultDto>>
{
    public async Task<Result<ReportResultDto>> Handle(RunReportQuery query, CancellationToken cancellationToken)
    {
        if (ReportDefinitions.Check(query.Definition) is { } error)
        {
            return error;
        }

        return await calculator.ComputeAsync(query.Definition!, cancellationToken);
    }
}
