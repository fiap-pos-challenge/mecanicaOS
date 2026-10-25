using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Identity.Invitations;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Identity.Commands.Logout;

public sealed class LogoutCommandHandler(IUserSessionRepository sessions, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken)
            || await sessions.GetByTokenHashAsync(InvitationToken.Hash(command.RefreshToken), cancellationToken) is not { } session)
        {
            return Result.Success();
        }

        session.Revoke(timeProvider.GetUtcNow());
        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
