using MediatR;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Reports.Queries.GetReportExport;

public sealed record GetReportExportQuery(Guid Id) : IRequest<Result<ExportJobDto>>;
