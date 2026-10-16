using Oficina.Application.Common.Pagination;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Application.Vehicles.Queries;

public interface IVehicleQueries
{
    Task<VehicleDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<VehicleDto>> ListAsync(PageRequest page, LicensePlate? plate, CancellationToken cancellationToken);
}
