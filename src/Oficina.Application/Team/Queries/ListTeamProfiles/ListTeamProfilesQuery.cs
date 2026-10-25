using MediatR;
using Oficina.Application.Team.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Team.Queries.ListTeamProfiles;

public sealed record ListTeamProfilesQuery(IReadOnlyList<Guid> UserIds) : IRequest<Result<TeamProfileListDto>>;
