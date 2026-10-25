using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Common.Pagination;

namespace Oficina.Application.Catalog.Queries;

public interface IPartQueries
{
    Task<PartDto?> GetByIdAsync(Guid id, bool includeCost, CancellationToken cancellationToken);

    Task<PagedResult<PartDto>> ListAsync(PageRequest page, CatalogFilter filter, bool includeCost, CancellationToken cancellationToken);
}
