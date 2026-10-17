using Oficina.Domain.Inventory.Entities;

namespace Oficina.Domain.Inventory.Repositories;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> DocumentExistsAsync(string document, Guid? exceptId, CancellationToken cancellationToken);

    void Add(Supplier supplier);
}

public interface IStockCountRepository
{
    Task<StockCount?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> HasOpenAsync(string location, CancellationToken cancellationToken);

    void Add(StockCount count);
}

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<long> NextNumberAsync(CancellationToken cancellationToken);

    void Add(PurchaseOrder order);
}
