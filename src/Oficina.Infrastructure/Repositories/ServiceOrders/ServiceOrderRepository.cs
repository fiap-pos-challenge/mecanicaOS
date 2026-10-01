using Microsoft.EntityFrameworkCore;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.Repositories;
using Oficina.Domain.ServiceOrders.ValueObjects;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.ServiceOrders;

public sealed class ServiceOrderRepository(OficinaDbContext context) : IServiceOrderRepository
{
    private const string NextNumberSql = "SELECT NEXT VALUE FOR [" + OficinaDbContext.ServiceOrderNumberSequence + "] AS [Value]";

    public Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.ServiceOrders
            .Include(order => order.ServiceItems)
            .Include(order => order.PartItems)
            .Include(order => order.Quotes)
            .Include(order => order.StatusHistory)
            .AsSplitQuery()
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task<ServiceOrderNumber> NextNumberAsync(CancellationToken cancellationToken)
    {
        var values = await context.Database.SqlQueryRaw<long>(NextNumberSql).ToListAsync(cancellationToken);
        return ServiceOrderNumber.Create(values.Single()).Value;
    }

    public Task<bool> HasOpenOrderForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        context.ServiceOrders.AnyAsync(order => order.VehicleId == vehicleId && order.Status != ServiceOrderStatus.Delivered, cancellationToken);

    public Task<bool> HasOpenOrderForCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        context.ServiceOrders.AnyAsync(order => order.CustomerId == customerId && order.Status != ServiceOrderStatus.Delivered, cancellationToken);

    public void Add(ServiceOrder serviceOrder) => context.ServiceOrders.Add(serviceOrder);
}
