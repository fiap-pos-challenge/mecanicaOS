using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Domain.Inventory.ValueObjects;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Inventory;

public sealed class PartRepository(OficinaDbContext context) : IPartRepository
{
    public Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Parts
            .Include(part => part.StockMovements)
            .SingleOrDefaultAsync(part => part.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Part>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        ids.Count == 0 ? [] : await context.Parts.Where(part => ids.Contains(part.Id)).ToListAsync(cancellationToken);

    public Task<bool> CodeExistsAsync(PartCode code, Guid? exceptId, CancellationToken cancellationToken) =>
        context.Parts.IgnoreQueryFilters()
            .AnyAsync(part => part.Code == code && part.DeletedAt == null && part.Id != exceptId, cancellationToken);

    public void Add(Part part) => context.Parts.Add(part);
}
