using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Users.Commands.ReactivateUser;

public sealed record ReactivateUserCommand(Guid Id) : IRequest<Result>;
