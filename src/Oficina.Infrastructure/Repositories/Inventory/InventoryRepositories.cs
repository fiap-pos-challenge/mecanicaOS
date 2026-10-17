using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Inventory;

public sealed class SupplierRepository(OficinaDbContext context) : ISupplierRepository
{
    public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Suppliers.SingleOrDefaultAsync(supplier => supplier.Id == id, cancellationToken);

    public Task<bool> DocumentExistsAsync(string document, Guid? exceptId, CancellationToken cancellationToken) =>
        context.Suppliers.AnyAsync(supplier => supplier.Document == document && supplier.Id != exceptId, cancellationToken);

    public void Add(Supplier supplier) => context.Suppliers.Add(supplier);
}

public sealed class StockCountRepository(OficinaDbContext context) : IStockCountRepository
{
    public Task<StockCount?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.StockCounts.SingleOrDefaultAsync(count => count.Id == id, cancellationToken);

    public Task<bool> HasOpenAsync(string location, CancellationToken cancellationToken) =>
        context.StockCounts.AnyAsync(count => count.Location == location && (count.Status == CountStatus.Open || count.Status == CountStatus.PendingApproval), cancellationToken);

    public void Add(StockCount count) => context.StockCounts.Add(count);
}

public sealed class PurchaseOrderRepository(OficinaDbContext context) : IPurchaseOrderRepository
{
    public const long FirstNumber = 1001;

    public Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.PurchaseOrders.SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task<long> NextNumberAsync(CancellationToken cancellationToken) =>
        (await context.PurchaseOrders.IgnoreQueryFilters().MaxAsync(order => (long?)order.Number, cancellationToken) ?? FirstNumber - 1) + 1;

    public void Add(PurchaseOrder order) => context.PurchaseOrders.Add(order);
}
