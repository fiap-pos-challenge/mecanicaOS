using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;

namespace Oficina.Domain.Identity.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken);

    Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken);

    void Add(User user);

    void Remove(User user);
}
