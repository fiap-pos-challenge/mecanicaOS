using MediatR;
using Oficina.Application.Team.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Team.Queries.GetProductivity;

public sealed class GetProductivityQueryHandler(ITeamQueries queries, TimeProvider timeProvider)
    : IRequestHandler<GetProductivityQuery, Result<ProductivityListDto>>
{
    public async Task<Result<ProductivityListDto>> Handle(GetProductivityQuery query, CancellationToken cancellationToken) =>
        new ProductivityListDto(query.PeriodDays, await queries.ProductivityAsync(query.UserIds.Distinct().ToList(), query.PeriodDays, timeProvider.GetUtcNow(), cancellationToken));
}
