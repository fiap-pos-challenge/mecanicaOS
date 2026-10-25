using Oficina.Domain.Inventory.Entities;

namespace Oficina.Domain.Inventory.Repositories;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> DocumentExistsAsync(string document, Guid? exceptId, CancellationToken cancellationToken);

    void Add(Supplier supplier);
}
