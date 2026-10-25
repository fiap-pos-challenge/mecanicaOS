using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Inventory;

public sealed class PurchaseOrderRepository(OficinaDbContext context) : IPurchaseOrderRepository
{
    public const long FirstNumber = 1001;

    public Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.PurchaseOrders.SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task<long> NextNumberAsync(CancellationToken cancellationToken) =>
        (await context.PurchaseOrders.IgnoreQueryFilters().MaxAsync(order => (long?)order.Number, cancellationToken) ?? FirstNumber - 1) + 1;

    public void Add(PurchaseOrder order) => context.PurchaseOrders.Add(order);
}
