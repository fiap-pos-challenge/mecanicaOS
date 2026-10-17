using Microsoft.EntityFrameworkCore;
using Oficina.Application.Jobs;
using Oficina.Application.Jobs.Dtos;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.Jobs;

public sealed class CustomerInsightQueries(OficinaDbContext context) : ICustomerInsightQueries
{
    private static readonly TimeSpan NoReturnAfter = TimeSpan.FromDays(90);

    public async Task<IReadOnlyList<CustomerSummaryDto>> SummariesAsync(IReadOnlyCollection<Guid> customerIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (customerIds.Count == 0)
        {
            return [];
        }

        var vehicles = await context.Vehicles.AsNoTracking().Where(vehicle => customerIds.Contains(vehicle.CustomerId))
            .GroupBy(vehicle => vehicle.CustomerId).Select(group => new { group.Key, Count = group.Count() }).ToListAsync(cancellationToken);
        var orders = await context.ServiceOrders.AsNoTracking().Where(order => customerIds.Contains(order.CustomerId))
            .Select(order => new { order.CustomerId, order.Status, order.CreatedAt, order.TotalAmount }).ToListAsync(cancellationToken);
        return customerIds.Select(id =>
        {
            var mine = orders.Where(order => order.CustomerId == id).ToList();
            var delivered = mine.Where(order => order.Status == ServiceOrderStatus.Delivered).ToList();
            var spent = delivered.Sum(order => order.TotalAmount.Amount);
            var open = mine.Count - delivered.Count;
            DateTimeOffset? last = mine.Count == 0 ? null : mine.Max(order => order.CreatedAt);
            var vehicleCount = vehicles.FirstOrDefault(vehicle => vehicle.Key == id)?.Count ?? 0;
            var situation = open > 0 ? "OpenServiceOrder" : last is { } visit && now - visit <= NoReturnAfter ? "Active" : "NoReturn";
            return new CustomerSummaryDto(id, vehicleCount, last, spent, delivered.Count == 0 ? 0 : Oficina.Application.Common.Numbers.Rounding.HalfUp(spent / delivered.Count, 2), mine.Count, open, situation, vehicleCount >= 3);
        }).ToList();
    }

    public async Task<IReadOnlyList<ServiceHistoryEntryDto>> HistoryAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var orders = await context.ServiceOrders.AsNoTracking().Include(order => order.ServiceItems)
            .Where(order => order.CustomerId == customerId).OrderByDescending(order => order.CreatedAt).ToListAsync(cancellationToken);
        var ids = orders.Select(order => order.Id).ToList();
        var paid = await context.PortalCharges.AsNoTracking().Where(charge => ids.Contains(charge.ServiceOrderId) && charge.Status == "Paid")
            .Select(charge => new { charge.ServiceOrderId, charge.Method }).ToListAsync(cancellationToken);
        return orders.Select(order => new ServiceHistoryEntryDto(
            order.Number.Value,
            order.ServiceItems.Count > 0 ? string.Join(", ", order.ServiceItems.Select(item => item.Description)) : order.ProblemDescription,
            order.TotalAmount.Amount,
            order.Status == ServiceOrderStatus.Delivered ? "Delivered" : "InProgress",
            order.CreatedAt,
            order.DeliveredAt,
            paid.FirstOrDefault(charge => charge.ServiceOrderId == order.Id)?.Method)).ToList();
    }

    public async Task<CustomerExport?> ExportAsync(Guid customerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (await context.Customers.AsNoTracking().SingleOrDefaultAsync(customer => customer.Id == customerId, cancellationToken) is not { } customer)
        {
            return null;
        }

        var vehicles = await context.Vehicles.AsNoTracking().Where(vehicle => vehicle.CustomerId == customerId).ToListAsync(cancellationToken);
        var orders = await context.ServiceOrders.AsNoTracking().Where(order => order.CustomerId == customerId).OrderBy(order => order.CreatedAt).ToListAsync(cancellationToken);
        var accesses = await (from access in context.SensitiveDataAccesses.AsNoTracking()
                              join user in context.Users.AsNoTracking() on access.UserId equals user.Id
                              where access.CustomerId == customerId
                              orderby access.OccurredAt
                              select new { access.OccurredAt, user.Name, access.Field, access.Reason }).ToListAsync(cancellationToken);
        var address = customer.Address is { } value
            ? new { value.ZipCode, value.Street, value.Number, value.Complement, value.District, value.City, value.State }
            : null;
        return new CustomerExport(
            customerId,
            now,
            new ExportCustomer(customer.Name, customer.Document.Value, customer.DocumentType.ToString(), customer.Email?.Value, customer.Phone.Value, address, customer.ConsentWhatsApp, customer.ConsentEmail, customer.ConsentUpdatedAt, customer.CreatedAt),
            vehicles.Select(vehicle => new ExportVehicle(vehicle.LicensePlate.Value, vehicle.Brand, vehicle.Model, vehicle.Year.Value)).ToList(),
            orders.Select(order => new ExportServiceOrder(order.Number.Value, order.Status.ToString(), order.CreatedAt, order.TotalAmount.Amount)).ToList(),
            accesses.Select(access => new ExportAccess(access.OccurredAt, access.Name, access.Field.ToString(), access.Reason.ToString())).ToList());
    }
}
