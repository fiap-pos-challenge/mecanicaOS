using Oficina.Domain.Customers.Entities;

namespace Oficina.Domain.Customers.Repositories;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> AnyForCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    void Add(Vehicle vehicle);
}
