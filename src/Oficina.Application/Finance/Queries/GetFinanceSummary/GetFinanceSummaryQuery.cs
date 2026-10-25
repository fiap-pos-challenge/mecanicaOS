using MediatR;
using Oficina.Application.Finance.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Finance.Queries.GetFinanceSummary;

public sealed record GetFinanceSummaryQuery(string? Period) : IRequest<Result<FinanceSummaryDto>>;
