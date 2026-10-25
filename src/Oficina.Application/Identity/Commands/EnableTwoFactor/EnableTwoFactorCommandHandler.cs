using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Sessions;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Identity.Commands.EnableTwoFactor;

public sealed class EnableTwoFactorCommandHandler(IUserRepository users, ICurrentUser currentUser, ISecretProtector protector, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IRequestHandler<EnableTwoFactorCommand, Result>
{
    public async Task<Result> Handle(EnableTwoFactorCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || await users.GetByIdAsync(userId, cancellationToken) is not { } user)
        {
            return CommonErrors.Unauthorized;
        }

        if (user.TwoFactorSecret is not { } stored || protector.Unprotect(stored) is not { } secret)
        {
            return IdentityErrors.TwoFactorNotSetUp;
        }

        if (!Totp.Verify(secret, command.Code, timeProvider.GetUtcNow()))
        {
            return IdentityErrors.InvalidTwoFactorCode with { Type = ErrorType.Validation };
        }

        user.EnableTwoFactor();
        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
