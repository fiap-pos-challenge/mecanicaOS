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
