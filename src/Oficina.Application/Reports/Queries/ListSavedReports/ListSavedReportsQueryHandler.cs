using MediatR;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Reports;

namespace Oficina.Application.Reports.Queries.ListSavedReports;

public sealed class ListSavedReportsQueryHandler(ISavedReportRepository reports) : IRequestHandler<ListSavedReportsQuery, Result<SavedReportListDto>>
{
    public async Task<Result<SavedReportListDto>> Handle(ListSavedReportsQuery query, CancellationToken cancellationToken) =>
        new SavedReportListDto((await reports.ListAsync(cancellationToken)).Select(report => report.ToDto()).ToList());
}
