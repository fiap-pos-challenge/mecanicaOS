using MediatR;
using Oficina.Application.Team.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Team.Queries.GetProductivity;

public sealed record GetProductivityQuery(IReadOnlyList<Guid> UserIds, int PeriodDays) : IRequest<Result<ProductivityListDto>>;
