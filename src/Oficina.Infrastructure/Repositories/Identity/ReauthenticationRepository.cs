using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Identity;

public sealed class ReauthenticationRepository(OficinaDbContext context) : IReauthenticationRepository
{
    public Task<Reauthentication?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.Reauthentications.SingleOrDefaultAsync(reauthentication => reauthentication.TokenHash == tokenHash, cancellationToken);

    public void Add(Reauthentication reauthentication) => context.Reauthentications.Add(reauthentication);
}
