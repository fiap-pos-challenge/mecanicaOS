using Microsoft.EntityFrameworkCore;
using Oficina.Application.Analytics;
using Oficina.Application.Dashboard;
using Oficina.Domain.Inventory.Enums;
using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.Analytics;

public sealed class AnalyticsQueries(OficinaDbContext context) : IAnalyticsQueries, ISupplierQueries
{
    private static readonly TimeSpan MaintenanceAfter = TimeSpan.FromDays(180);
    private static readonly TimeSpan MaintenanceWindow = TimeSpan.FromDays(30);

    public async Task<IReadOnlyList<SaleFact>> SalesAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var orders = await context.ServiceOrders.AsNoTracking().Include(order => order.ServiceItems).Include(order => order.PartItems)
            .Where(order => order.Status == ServiceOrderStatus.Delivered && order.DeliveredAt >= from && order.DeliveredAt <= to)
            .ToListAsync(cancellationToken);
        if (orders.Count == 0)
        {
            return [];
        }

        var customerIds = orders.Select(order => order.CustomerId).Distinct().ToList();
        var mechanicIds = orders.Where(order => order.MechanicId != null).Select(order => order.MechanicId!.Value).Distinct().ToList();
        var orderIds = orders.Select(order => order.Id).ToList();
        var serviceIds = orders.SelectMany(order => order.ServiceItems.Select(item => item.ServiceId)).Distinct().ToList();
        var partIds = orders.SelectMany(order => order.PartItems.Select(item => item.PartId)).Distinct().ToList();
        var customers = await context.Customers.AsNoTracking().IgnoreQueryFilters().Where(customer => customerIds.Contains(customer.Id)).Select(customer => new { customer.Id, customer.Name }).ToListAsync(cancellationToken);
        var mechanics = await context.Users.AsNoTracking().IgnoreQueryFilters().Where(user => mechanicIds.Contains(user.Id)).Select(user => new { user.Id, user.Name }).ToListAsync(cancellationToken);
        var commissions = await context.Services.AsNoTracking().IgnoreQueryFilters().Where(service => serviceIds.Contains(service.Id)).Select(service => new { service.Id, service.CommissionPercent }).ToListAsync(cancellationToken);
        var partRows = await context.Parts.AsNoTracking().IgnoreQueryFilters().Where(part => partIds.Contains(part.Id)).ToListAsync(cancellationToken);
        var payments = await context.PortalCharges.AsNoTracking().Where(charge => orderIds.Contains(charge.ServiceOrderId) && charge.Status == "Paid").Select(charge => new { charge.ServiceOrderId, charge.Method }).ToListAsync(cancellationToken);
        return orders.Select(order => new SaleFact(
            order.Id,
            order.Number.Value,
            order.DeliveredAt!.Value,
            customers.FirstOrDefault(customer => customer.Id == order.CustomerId)?.Name ?? "Cliente",
            order.MechanicId is { } mechanicId ? mechanics.FirstOrDefault(user => user.Id == mechanicId)?.Name ?? "Sem mecânico" : "Sem mecânico",
            payments.FirstOrDefault(payment => payment.ServiceOrderId == order.Id)?.Method ?? "Balcão",
            order.TotalAmount.Amount,
            order.ServiceItems.Select(item => new SaleServiceLine(item.Description, item.Total.Amount, item.Total.Amount * (commissions.FirstOrDefault(service => service.Id == item.ServiceId)?.CommissionPercent ?? 0) / 100)).ToList(),
            order.PartItems.Select(item =>
            {
                var part = partRows.FirstOrDefault(row => row.Id == item.PartId);
                var category = part?.Type == PartType.Supply ? "Insumos" : "Peças";
                return new SalePartLine(item.Description, category, item.Quantity.Value, item.Total.Amount, item.Quantity.Value * (part?.Cost.Amount ?? 0));
            }).ToList())).ToList();
    }

    public async Task<OperationsFact> OperationsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var open = await context.ServiceOrders.AsNoTracking().Include(order => order.PartItems).Where(order => order.Status != ServiceOrderStatus.Delivered).ToListAsync(cancellationToken);
        var vehicleIds = open.Select(order => order.VehicleId).Distinct().ToList();
        var vehicles = await context.Vehicles.AsNoTracking().IgnoreQueryFilters().Where(vehicle => vehicleIds.Contains(vehicle.Id)).ToListAsync(cancellationToken);
        var lowParts = await context.Parts.AsNoTracking().Where(part => part.IsActive && part.MinimumStock > 0 && part.StockQuantity < part.MinimumStock).ToListAsync(cancellationToken);
        var allPartIds = open.SelectMany(order => order.PartItems.Select(item => item.PartId)).Distinct().ToList();
        var stock = await context.Parts.AsNoTracking().IgnoreQueryFilters().Where(part => allPartIds.Contains(part.Id)).Select(part => new { part.Id, part.StockQuantity }).ToListAsync(cancellationToken);
        var waitingParts = new[] { ServiceOrderStatus.Received, ServiceOrderStatus.InDiagnosis, ServiceOrderStatus.WaitingApproval };
        var openFacts = open.Select(order =>
        {
            var vehicle = vehicles.FirstOrDefault(row => row.Id == order.VehicleId);
            var missing = waitingParts.Contains(order.Status) && order.PartItems.Any(item => (stock.FirstOrDefault(row => row.Id == item.PartId)?.StockQuantity ?? 0) < item.Quantity.Value);
            return new OpenOrderFact(order.Id, order.Number.Value, order.Status.ToString(), vehicle is null ? string.Empty : $"{vehicle.Brand} {vehicle.Model}", order.PromisedAt, missing, order.TotalAmount.Amount);
        }).ToList();
        var lowFacts = lowParts.Select(part => new LowStockFact(part.Id, part.Name, part.StockQuantity, part.MinimumStock, part.UnitOfMeasure,
            open.Count(order => waitingParts.Contains(order.Status) && order.PartItems.Any(item => item.PartId == part.Id)))).ToList();
        var estimates = await context.Estimates.AsNoTracking().ToListAsync(cancellationToken);
        var latest = estimates.GroupBy(estimate => estimate.Number).Select(group => group.OrderByDescending(estimate => estimate.Version).First()).ToList();
        var pending = latest.Where(estimate => estimate.Status == EstimateStatus.Pending && estimate.Delivery is { SentAt: not null })
            .Select(estimate => new PendingQuoteFact(estimate.Id, estimate.Number, estimate.Delivery!.SentAt!.Value, estimate.Delivery.Channel)).ToList();
        var answered = estimates.Where(estimate => estimate.Status is EstimateStatus.Approved or EstimateStatus.Rejected)
            .Select(estimate => new AnsweredQuoteFact(estimate.Status, estimate.UpdatedAt ?? estimate.CreatedAt)).ToList();
        var lastVisits = await context.ServiceOrders.AsNoTracking().GroupBy(order => order.CustomerId)
            .Select(group => group.Max(order => order.CreatedAt)).ToListAsync(cancellationToken);
        var due = lastVisits.Count(visit => now - visit >= MaintenanceAfter - MaintenanceWindow);
        return new OperationsFact(openFacts, pending, lowFacts, answered, due);
    }

    public async Task<Guid?> PreferredAsync(string? name, CancellationToken cancellationToken)
    {
        var active = context.Suppliers.AsNoTracking().Where(supplier => supplier.IsActive);
        if (!string.IsNullOrWhiteSpace(name) && await active.FirstOrDefaultAsync(supplier => supplier.Name == name.Trim(), cancellationToken) is { } preferred)
        {
            return preferred.Id;
        }

        return await active.OrderBy(supplier => supplier.Name).Select(supplier => (Guid?)supplier.Id).FirstOrDefaultAsync(cancellationToken);
    }
}
