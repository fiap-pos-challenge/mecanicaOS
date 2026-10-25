using Oficina.Domain.Catalog.Entities;

namespace Oficina.Domain.Catalog.Repositories;

public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Service service);

    Task<IReadOnlyList<Service>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
