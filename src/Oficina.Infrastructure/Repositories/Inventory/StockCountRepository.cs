using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Inventory;

public sealed class StockCountRepository(OficinaDbContext context) : IStockCountRepository
{
    public Task<StockCount?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.StockCounts.SingleOrDefaultAsync(count => count.Id == id, cancellationToken);

    public Task<bool> HasOpenAsync(string location, CancellationToken cancellationToken) =>
        context.StockCounts.AnyAsync(count => count.Location == location && (count.Status == CountStatus.Open || count.Status == CountStatus.PendingApproval), cancellationToken);

    public void Add(StockCount count) => context.StockCounts.Add(count);
}
