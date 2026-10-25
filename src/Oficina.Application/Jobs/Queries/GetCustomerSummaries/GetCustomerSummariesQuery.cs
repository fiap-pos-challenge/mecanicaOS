using MediatR;
using Oficina.Application.Jobs.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Jobs.Queries.GetCustomerSummaries;

public sealed record GetCustomerSummariesQuery(IReadOnlyList<Guid>? CustomerIds) : IRequest<Result<CustomerSummaryListDto>>;
