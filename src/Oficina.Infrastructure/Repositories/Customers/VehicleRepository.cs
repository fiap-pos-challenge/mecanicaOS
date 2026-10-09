using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Customers;

public sealed class VehicleRepository(OficinaDbContext context) : IVehicleRepository
{
    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Vehicles.SingleOrDefaultAsync(vehicle => vehicle.Id == id, cancellationToken);

    public Task<bool> AnyForCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        context.Vehicles.AnyAsync(vehicle => vehicle.CustomerId == customerId, cancellationToken);

    public void Add(Vehicle vehicle) => context.Vehicles.Add(vehicle);

    public Task<Vehicle?> GetByLicensePlate(string licensePlate, CancellationToken cancellationToken)=>
        context.Vehicles.SingleOrDefaultAsync(vehicle => vehicle.LicensePlate.Value == licensePlate, cancellationToken);
}
