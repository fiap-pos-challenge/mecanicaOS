using Oficina.Application.Common.Pagination;
using Oficina.Application.Vehicles.Dto;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Application.Vehicles.Queries;

public interface IVehiclesQueries
{
    Task<VehicleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<VehicleDto?> GetByLicensePlate(LicensePlate licensePlate, CancellationToken cancellationToken);

    Task<PagedResult<VehicleDto>> ListAsync(PageRequest page, CancellationToken cancellationToken);
}
