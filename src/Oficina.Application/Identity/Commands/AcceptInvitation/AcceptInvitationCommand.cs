using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Identity.Commands.AcceptInvitation;

public sealed record AcceptInvitationCommand(string Token, string Password) : IRequest<Result>;
