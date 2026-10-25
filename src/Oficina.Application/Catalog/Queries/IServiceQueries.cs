using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Common.Pagination;

namespace Oficina.Application.Catalog.Queries;

public interface IServiceQueries
{
    Task<ServiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ServiceDto>> ListAsync(PageRequest page, CatalogFilter filter, CancellationToken cancellationToken);
}
