using MediatR;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Reports.Queries.ListSavedReports;

public sealed record ListSavedReportsQuery : IRequest<Result<SavedReportListDto>>;
