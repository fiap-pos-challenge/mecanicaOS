using MediatR;
using Oficina.Application.Identity.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Identity.Queries.InspectInvitation;

public sealed record InspectInvitationQuery(string Token) : IRequest<Result<InvitationDto>>;
