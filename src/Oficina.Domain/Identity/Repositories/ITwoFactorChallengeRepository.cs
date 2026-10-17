using Oficina.Domain.Identity.Entities;

namespace Oficina.Domain.Identity.Repositories;

public interface ITwoFactorChallengeRepository
{
    Task<TwoFactorChallenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(TwoFactorChallenge challenge);
}
