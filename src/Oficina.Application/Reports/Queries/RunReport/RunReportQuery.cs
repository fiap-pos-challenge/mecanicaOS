using MediatR;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Reports.Queries.RunReport;

public sealed record RunReportQuery(ReportDefinitionDto? Definition) : IRequest<Result<ReportResultDto>>;
