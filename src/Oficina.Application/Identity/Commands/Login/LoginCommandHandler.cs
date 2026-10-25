using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Dtos;
using Oficina.Application.Identity.Sessions;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Identity.Commands.Login;

public sealed class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    SessionIssuer sessionIssuer,
    ITwoFactorChallengeRepository challenges,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    public async Task<Result<LoginResult>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var email = Email.Create(command.Email);
        var user = email.IsSuccess ? await users.GetByEmailAsync(email.Value, cancellationToken) : null;
        var passwordMatches = passwordHasher.Verify(command.Password, user?.PasswordHash ?? passwordHasher.DummyHash);
        if (user is null || user.IsDeleted || user.HasPendingInvitation)
        {
            return IdentityErrors.InvalidCredentials;
        }

        if (user.IsLockedOut(now))
        {
            return IdentityErrors.AccountLocked(user.LockoutRemaining(now));
        }

        if (!passwordMatches)
        {
            return await RegisterFailureAsync(user, now, cancellationToken);
        }

        if (!user.IsActive)
        {
            return IdentityErrors.AccountDisabled;
        }

        user.RegisterSuccessfulLogin();
        LoginResult result;
        if (user.TwoFactorEnabled)
        {
            var challenge = TwoFactorChallenge.Open(user.Id, now);
            challenges.Add(challenge);
            result = new LoginResult(new LoginResponse(null, null, true, challenge.Id), null, null);
        }
        else
        {
            result = sessionIssuer.Start(user, command.Device, now);
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return result;
    }

    private async Task<Result<LoginResult>> RegisterFailureAsync(User user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        user.RegisterFailedLogin(now);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return user.IsLockedOut(now) ? IdentityErrors.AccountLocked(user.LockoutRemaining(now)) : IdentityErrors.InvalidCredentials;
    }
}
