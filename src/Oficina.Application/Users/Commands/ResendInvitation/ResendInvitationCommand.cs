using MediatR;
using Oficina.Application.Users.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Users.Commands.ResendInvitation;

public sealed record ResendInvitationCommand(Guid Id) : IRequest<Result<UserDto>>;
