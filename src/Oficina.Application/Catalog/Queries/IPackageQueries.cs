using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Common.Pagination;

namespace Oficina.Application.Catalog.Queries;

public interface IPackageQueries
{
    Task<PackageDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<PackageDto>> ListAsync(PageRequest page, CatalogFilter filter, CancellationToken cancellationToken);
}
