using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Domain.Customers.Repositories;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> AnyForCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    Task<Vehicle?> GetByLicensePlate(LicensePlate licensePlate, CancellationToken cancellationToken);

    void Add(Vehicle vehicle);

    void Remove(Vehicle vehicle);
}
