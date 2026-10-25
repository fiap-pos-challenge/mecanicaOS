using MediatR;
using Oficina.Application.Team.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Team.Queries.GetTeamProfile;

public sealed class GetTeamProfileQueryHandler(ITeamQueries queries) : IRequestHandler<GetTeamProfileQuery, Result<TeamProfileDto>>
{
    public async Task<Result<TeamProfileDto>> Handle(GetTeamProfileQuery query, CancellationToken cancellationToken) =>
        (await queries.ListProfilesAsync([query.UserId], cancellationToken))[0];
}
