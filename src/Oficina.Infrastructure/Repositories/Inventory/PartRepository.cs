using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Inventory;

public sealed class PartRepository(OficinaDbContext context) : IPartRepository
{
    public Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Parts
            .Include(part => part.StockMovements)
            .SingleOrDefaultAsync(part => part.Id == id, cancellationToken);

    public void Add(Part part) => context.Parts.Add(part);
}
