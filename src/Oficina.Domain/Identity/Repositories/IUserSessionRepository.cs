using Oficina.Domain.Identity.Entities;

namespace Oficina.Domain.Identity.Repositories;

public interface IUserSessionRepository
{
    Task<UserSession?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(UserSession session);
}
