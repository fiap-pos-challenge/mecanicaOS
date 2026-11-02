using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Users.Commands.DeactivateUser;

public sealed record DeactivateUserCommand(Guid Id) : IRequest<Result>;
