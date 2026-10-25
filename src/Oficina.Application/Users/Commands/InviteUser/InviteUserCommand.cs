using MediatR;
using Oficina.Application.Users.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Enums;

namespace Oficina.Application.Users.Commands.InviteUser;

public sealed record InviteUserCommand(string Name, string Email, Role Role) : IRequest<Result<UserDto>>;
