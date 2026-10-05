using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.ValueObjects;

namespace Oficina.Domain.ServiceOrders.Repositories;

public interface IServiceOrderRepository
{
    Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceOrderNumber> NextNumberAsync(CancellationToken cancellationToken);

    Task<bool> HasOpenOrderForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);

    Task<bool> HasOpenOrderForCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    void Add(ServiceOrder serviceOrder);
}
