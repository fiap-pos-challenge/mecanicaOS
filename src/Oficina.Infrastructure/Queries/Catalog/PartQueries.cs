using Microsoft.EntityFrameworkCore;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Mappers;
using Oficina.Application.Catalog.Queries;
using Oficina.Application.Common.Pagination;
using Oficina.Domain.Inventory.ValueObjects;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.Catalog;

public sealed class PartQueries(OficinaDbContext context) : IPartQueries
{
    public async Task<PartDto?> GetByIdAsync(Guid id, bool includeCost, CancellationToken cancellationToken)
    {
        var part = await context.Parts.AsNoTracking().SingleOrDefaultAsync(part => part.Id == id, cancellationToken);
        return part?.ToDto(includeCost);
    }

    public async Task<PagedResult<PartDto>> ListAsync(PageRequest page, CatalogFilter filter, bool includeCost, CancellationToken cancellationToken)
    {
        var parts = context.Parts.AsNoTracking();
        if (filter.IsActive is { } isActive)
        {
            parts = parts.Where(part => part.IsActive == isActive);
        }

        if (filter.Term is { } term)
        {
            var code = PartCode.Create(term);
            parts = code.IsSuccess
                ? parts.Where(part => part.Name.Contains(term) || part.Code == code.Value)
                : parts.Where(part => part.Name.Contains(term));
        }

        var totalItems = await parts.CountAsync(cancellationToken);
        var items = await parts
            .OrderBy(part => part.Name)
            .ThenBy(part => part.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PartDto>(items.Select(part => part.ToDto(includeCost)).ToList(), page.Page, page.PageSize, totalItems);
    }
}
