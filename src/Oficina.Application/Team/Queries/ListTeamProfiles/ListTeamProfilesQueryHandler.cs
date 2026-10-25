using MediatR;
using Oficina.Application.Team.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Team.Queries.ListTeamProfiles;

public sealed class ListTeamProfilesQueryHandler(ITeamQueries queries) : IRequestHandler<ListTeamProfilesQuery, Result<TeamProfileListDto>>
{
    public async Task<Result<TeamProfileListDto>> Handle(ListTeamProfilesQuery query, CancellationToken cancellationToken) =>
        new TeamProfileListDto(await queries.ListProfilesAsync(query.UserIds.Distinct().ToList(), cancellationToken));
}
