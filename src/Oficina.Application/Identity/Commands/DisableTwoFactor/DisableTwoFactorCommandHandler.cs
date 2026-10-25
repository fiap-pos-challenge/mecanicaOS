using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Sessions;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Identity.Commands.DisableTwoFactor;

public sealed class DisableTwoFactorCommandHandler(IUserRepository users, ICurrentUser currentUser, ReauthenticationGuard guard, IUnitOfWork unitOfWork)
    : IRequestHandler<DisableTwoFactorCommand, Result>
{
    public async Task<Result> Handle(DisableTwoFactorCommand command, CancellationToken cancellationToken)
    {
        if ((await guard.ConsumeAsync(command.ReauthToken, cancellationToken)).Error is { } reauthError)
        {
            return reauthError;
        }

        if (currentUser.UserId is not { } userId || await users.GetByIdAsync(userId, cancellationToken) is not { } user)
        {
            return CommonErrors.Unauthorized;
        }

        user.DisableTwoFactor();
        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
