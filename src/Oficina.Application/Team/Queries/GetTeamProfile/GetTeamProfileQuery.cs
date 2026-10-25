using MediatR;
using Oficina.Application.Team.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Team.Queries.GetTeamProfile;

public sealed record GetTeamProfileQuery(Guid UserId) : IRequest<Result<TeamProfileDto>>;
