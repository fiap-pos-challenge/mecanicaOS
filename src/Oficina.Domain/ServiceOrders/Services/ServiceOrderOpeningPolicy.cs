using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Domain.ServiceOrders.Services;

public sealed class ServiceOrderOpeningPolicy(IServiceOrderRepository serviceOrders)
{
    public async Task<Result> EnsureCanOpenAsync(Customer customer, Vehicle vehicle, CancellationToken cancellationToken)
    {
        if (vehicle.CustomerId != customer.Id)
        {
            return ServiceOrderErrors.VehicleNotOwnedByCustomer;
        }

        var hasOpenOrder = await serviceOrders.HasOpenOrderForVehicleAsync(vehicle.Id, cancellationToken);
        return hasOpenOrder ? ServiceOrderErrors.VehicleHasOpenOrder : Result.Success();
    }
}
