using Oficina.Application.Common.Pagination;
using Oficina.Application.Inventory.Dtos;

namespace Oficina.Application.Inventory.Queries;

public interface IPartQueries
{
    Task<PartDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<PartDto>> ListAsync(PageRequest page, bool includeInactive, bool belowMinimumOnly, CancellationToken cancellationToken);
}
