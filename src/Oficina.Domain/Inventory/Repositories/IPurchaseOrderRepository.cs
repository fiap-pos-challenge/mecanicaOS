using Oficina.Domain.Inventory.Entities;

namespace Oficina.Domain.Inventory.Repositories;

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<long> NextNumberAsync(CancellationToken cancellationToken);

    void Add(PurchaseOrder order);
}
