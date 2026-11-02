using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Domain.Customers.Services;

public sealed class RegistrationDeletionPolicy(IServiceOrderRepository serviceOrders, IVehicleRepository vehicles)
{
    public async Task<Result> EnsureCanDeleteCustomerAsync(Customer customer, CancellationToken cancellationToken)
    {
        if (await serviceOrders.HasOpenOrderForCustomerAsync(customer.Id, cancellationToken))
        {
            return CustomerErrors.HasOpenServiceOrder;
        }

        var hasActiveVehicles = await vehicles.AnyForCustomerAsync(customer.Id, cancellationToken);
        return hasActiveVehicles ? CustomerErrors.HasActiveVehicles : Result.Success();
    }

    public async Task<Result> EnsureCanDeleteVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        var hasOpenOrder = await serviceOrders.HasOpenOrderForVehicleAsync(vehicle.Id, cancellationToken);
        return hasOpenOrder ? VehicleErrors.HasOpenServiceOrder : Result.Success();
    }
}
