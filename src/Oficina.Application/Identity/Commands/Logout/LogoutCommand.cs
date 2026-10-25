using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Identity.Commands.Logout;

public sealed record LogoutCommand(string? RefreshToken) : IRequest<Result>;
