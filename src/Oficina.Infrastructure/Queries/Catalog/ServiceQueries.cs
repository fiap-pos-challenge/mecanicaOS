using Microsoft.EntityFrameworkCore;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Mappers;
using Oficina.Application.Catalog.Queries;
using Oficina.Application.Common.Pagination;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.Catalog;

public sealed class ServiceQueries(OficinaDbContext context) : IServiceQueries
{
    public async Task<ServiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var service = await context.Services.AsNoTracking().SingleOrDefaultAsync(service => service.Id == id, cancellationToken);
        return service?.ToDto();
    }

    public async Task<PagedResult<ServiceDto>> ListAsync(PageRequest page, bool includeInactive, CancellationToken cancellationToken)
    {
        var services = context.Services.AsNoTracking().Where(service => includeInactive || service.IsActive);
        var totalItems = await services.CountAsync(cancellationToken);
        var items = await services
            .OrderBy(service => service.Name)
            .ThenBy(service => service.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ServiceDto>(items.Select(service => service.ToDto()).ToList(), page.Page, page.PageSize, totalItems);
    }
}
