using Oficina.Domain.Identity.Entities;

namespace Oficina.Domain.Identity.Repositories;

public interface IReauthenticationRepository
{
    Task<Reauthentication?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    void Add(Reauthentication reauthentication);
}
