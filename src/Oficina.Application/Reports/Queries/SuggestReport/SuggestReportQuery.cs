using MediatR;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Reports.Queries.SuggestReport;

public sealed record SuggestReportQuery(string? Question) : IRequest<Result<ReportSuggestionDto>>;
