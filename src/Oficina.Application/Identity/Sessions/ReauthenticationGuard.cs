using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Invitations;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Identity.Sessions;

public sealed class ReauthenticationGuard(IReauthenticationRepository reauthentications, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<Result> ConsumeAsync(string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || currentUser.UserId is not { } userId)
        {
            return IdentityErrors.ReauthenticationRequired;
        }

        var reauthentication = await reauthentications.GetByTokenHashAsync(InvitationToken.Hash(token.Trim()), cancellationToken);
        return reauthentication is not null && reauthentication.TryConsume(userId, timeProvider.GetUtcNow())
            ? Result.Success()
            : IdentityErrors.ReauthenticationRequired;
    }
}
