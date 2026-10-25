using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Identity;

public sealed class TwoFactorChallengeRepository(OficinaDbContext context) : ITwoFactorChallengeRepository
{
    public Task<TwoFactorChallenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.TwoFactorChallenges.SingleOrDefaultAsync(challenge => challenge.Id == id, cancellationToken);

    public void Add(TwoFactorChallenge challenge) => context.TwoFactorChallenges.Add(challenge);
}
