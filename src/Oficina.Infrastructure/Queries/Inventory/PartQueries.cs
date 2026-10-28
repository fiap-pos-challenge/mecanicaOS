using Microsoft.EntityFrameworkCore;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Inventory.Dtos;
using Oficina.Application.Inventory.Mappers;
using Oficina.Application.Inventory.Queries;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.Inventory;

public sealed class PartQueries(OficinaDbContext context) : IPartQueries
{
    public async Task<PartDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var part = await context.Parts.AsNoTracking()
            .Include(part => part.StockMovements.OrderBy(movement => movement.CreatedAt).ThenBy(movement => movement.Id))
            .SingleOrDefaultAsync(part => part.Id == id, cancellationToken);
        return part?.ToDetailDto();
    }

    public async Task<PagedResult<PartDto>> ListAsync(PageRequest page, bool includeInactive, bool belowMinimumOnly, CancellationToken cancellationToken)
    {
        var parts = context.Parts.AsNoTracking().Where(part => includeInactive || part.IsActive);
        if (belowMinimumOnly)
        {
            parts = parts.Where(part => part.StockQuantity < part.MinimumStock);
        }

        var totalItems = await parts.CountAsync(cancellationToken);
        var items = await parts
            .OrderBy(part => part.Name)
            .ThenBy(part => part.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PartDto>(items.Select(part => part.ToDto()).ToList(), page.Page, page.PageSize, totalItems);
    }
}
