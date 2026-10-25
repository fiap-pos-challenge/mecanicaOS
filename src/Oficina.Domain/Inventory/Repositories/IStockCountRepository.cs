using Oficina.Domain.Inventory.Entities;

namespace Oficina.Domain.Inventory.Repositories;

public interface IStockCountRepository
{
    Task<StockCount?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> HasOpenAsync(string location, CancellationToken cancellationToken);

    void Add(StockCount count);
}
