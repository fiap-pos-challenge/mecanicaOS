using MediatR;
using Oficina.Application.Dashboard;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Reports.Queries.SuggestReport;

public sealed class SuggestReportQueryHandler(TimeProvider timeProvider) : IRequestHandler<SuggestReportQuery, Result<ReportSuggestionDto>>
{
    public Task<Result<ReportSuggestionDto>> Handle(SuggestReportQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(ReportSuggester.Suggest(query.Question, DateOnly.FromDateTime(timeProvider.GetUtcNow().ToOffset(DashboardClock.Brazil).DateTime)));
}
