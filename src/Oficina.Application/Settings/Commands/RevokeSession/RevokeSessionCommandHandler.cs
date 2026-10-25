using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Sessions;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Repositories;
using Oficina.Domain.Settings.Errors;

namespace Oficina.Application.Settings.Commands.RevokeSession;

public sealed class RevokeSessionCommandHandler(
    IUserSessionRepository sessions,
    SettingsAudit audit,
    ReauthenticationGuard guard,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<RevokeSessionCommand, Result>
{
    public async Task<Result> Handle(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (command.Id == currentUser.SessionId || await sessions.GetByIdAsync(command.Id, cancellationToken) is not { } session || !session.IsActive(now))
        {
            return SettingsErrors.SessionNotFound;
        }

        if ((await guard.ConsumeAsync(command.Reauthentication, cancellationToken)).Error is { } guardError)
        {
            return guardError;
        }

        session.Revoke(now);
        await audit.RecordAsync(SettingsAudit.Security, SettingsMessages.SessionRevoked, session.Device, SettingsMessages.SessionRevokedDetail, cancellationToken);
        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
