using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Enums;

namespace Oficina.Domain.Catalog.Repositories;

public interface IPackageRepository
{
    Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListActiveNamesUsingAsync(CatalogItemKind kind, Guid itemId, CancellationToken cancellationToken);

    void Add(Package package);
}
