using MediatR;
using Oficina.Application.Identity.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Identity.Commands.VerifyTwoFactor;

public sealed record VerifyTwoFactorCommand(Guid ChallengeId, string Code, string? Device = null) : IRequest<Result<LoginResult>>;
