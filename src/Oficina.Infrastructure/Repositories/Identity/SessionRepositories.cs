using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Identity;

public sealed class UserSessionRepository(OficinaDbContext context) : IUserSessionRepository
{
    public Task<UserSession?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.UserSessions.SingleOrDefaultAsync(session => session.TokenHash == tokenHash, cancellationToken);

    public Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.UserSessions.SingleOrDefaultAsync(session => session.Id == id, cancellationToken);

    public void Add(UserSession session) => context.UserSessions.Add(session);
}

public sealed class TwoFactorChallengeRepository(OficinaDbContext context) : ITwoFactorChallengeRepository
{
    public Task<TwoFactorChallenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.TwoFactorChallenges.SingleOrDefaultAsync(challenge => challenge.Id == id, cancellationToken);

    public void Add(TwoFactorChallenge challenge) => context.TwoFactorChallenges.Add(challenge);
}

public sealed class ReauthenticationRepository(OficinaDbContext context) : IReauthenticationRepository
{
    public Task<Reauthentication?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.Reauthentications.SingleOrDefaultAsync(reauthentication => reauthentication.TokenHash == tokenHash, cancellationToken);

    public void Add(Reauthentication reauthentication) => context.Reauthentications.Add(reauthentication);
}
