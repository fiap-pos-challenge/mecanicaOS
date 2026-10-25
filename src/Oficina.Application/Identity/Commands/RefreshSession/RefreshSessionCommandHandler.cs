using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Identity.Dtos;
using Oficina.Application.Identity.Invitations;
using Oficina.Application.Identity.Sessions;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Identity.Commands.RefreshSession;

public sealed class RefreshSessionCommandHandler(
    IUserSessionRepository sessions,
    IUserRepository users,
    SessionIssuer sessionIssuer,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<RefreshSessionCommand, Result<LoginResult>>
{
    public async Task<Result<LoginResult>> Handle(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (string.IsNullOrWhiteSpace(command.RefreshToken)
            || await sessions.GetByTokenHashAsync(InvitationToken.Hash(command.RefreshToken), cancellationToken) is not { } session
            || !session.IsActive(now))
        {
            return IdentityErrors.SessionExpired;
        }

        if (await users.GetByIdAsync(session.UserId, cancellationToken) is not { CanLogin: true } user)
        {
            session.Revoke(now);
            return (await unitOfWork.CommitAsync(cancellationToken)).Error ?? IdentityErrors.SessionExpired;
        }

        var result = sessionIssuer.Rotate(user, session, now);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return result;
    }
}
