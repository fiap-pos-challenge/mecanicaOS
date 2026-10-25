using MediatR;
using Oficina.Application.Users.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Enums;

namespace Oficina.Application.Users.Commands.CreateUser;

public sealed record CreateUserCommand(string Name, string Email, string Password, Role Role) : IRequest<Result<UserDto>>;
