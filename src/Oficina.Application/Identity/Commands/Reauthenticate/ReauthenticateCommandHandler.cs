using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Dtos;
using Oficina.Application.Identity.Invitations;
using Oficina.Application.Identity.Sessions;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Identity.Commands.Reauthenticate;

public sealed class ReauthenticateCommandHandler(
    IUserRepository users,
    IReauthenticationRepository reauthentications,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    ISecretProtector protector,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<ReauthenticateCommand, Result<ReauthenticationDto>>
{
    public async Task<Result<ReauthenticationDto>> Handle(ReauthenticateCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (currentUser.UserId is not { } userId || await users.GetByIdAsync(userId, cancellationToken) is not { } user)
        {
            return CommonErrors.Unauthorized;
        }

        var proven = command.Password is { Length: > 0 } password
            ? user.PasswordHash is { } hash && passwordHasher.Verify(password, hash)
            : command.Code is { Length: > 0 } code && user.TwoFactorEnabled && user.TwoFactorSecret is { } stored && protector.Unprotect(stored) is { } secret && Totp.Verify(secret, code, now);
        if (!proven)
        {
            return IdentityErrors.ReauthenticationFailed;
        }

        var token = InvitationToken.Generate();
        reauthentications.Add(Reauthentication.Issue(user.Id, InvitationToken.Hash(token), now));
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return new ReauthenticationDto(token);
    }
}
