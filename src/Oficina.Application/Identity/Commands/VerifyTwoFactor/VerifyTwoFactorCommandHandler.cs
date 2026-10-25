using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Dtos;
using Oficina.Application.Identity.Sessions;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Identity.Commands.VerifyTwoFactor;

public sealed class VerifyTwoFactorCommandHandler(
    ITwoFactorChallengeRepository challenges,
    IUserRepository users,
    SessionIssuer sessionIssuer,
    ISecretProtector protector,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<VerifyTwoFactorCommand, Result<LoginResult>>
{
    public async Task<Result<LoginResult>> Handle(VerifyTwoFactorCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (await challenges.GetByIdAsync(command.ChallengeId, cancellationToken) is not { } challenge || !challenge.IsOpen(now))
        {
            return IdentityErrors.InvalidTwoFactorCode;
        }

        var user = await users.GetByIdAsync(challenge.UserId, cancellationToken);
        if (user is null || !user.CanLogin || user.TwoFactorSecret is not { } stored || protector.Unprotect(stored) is not { } secret || !Totp.Verify(secret, command.Code, now))
        {
            challenge.RegisterFailure();
            return (await unitOfWork.CommitAsync(cancellationToken)).Error ?? IdentityErrors.InvalidTwoFactorCode;
        }

        challenge.Complete(now);
        var result = sessionIssuer.Start(user, command.Device, now);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return result;
    }
}
