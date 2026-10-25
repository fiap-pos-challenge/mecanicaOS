using MediatR;
using Oficina.Application.Users.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Enums;

namespace Oficina.Application.Users.Commands.UpdateUser;

public sealed record UpdateUserCommand(Guid Id, string Name, Role Role) : IRequest<Result<UserDto>>;
