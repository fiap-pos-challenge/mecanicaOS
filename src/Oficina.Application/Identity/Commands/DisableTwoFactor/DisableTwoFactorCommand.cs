using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Identity.Commands.DisableTwoFactor;

public sealed record DisableTwoFactorCommand(string? ReauthToken) : IRequest<Result>;
