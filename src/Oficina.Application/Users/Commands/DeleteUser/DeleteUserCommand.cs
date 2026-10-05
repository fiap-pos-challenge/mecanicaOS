using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Users.Commands.DeleteUser;

public sealed record DeleteUserCommand(Guid Id) : IRequest<Result>;
