using Microsoft.EntityFrameworkCore;
using Oficina.Application.Quotes.Dtos;
using Oficina.Application.Quotes.Queries;
using Oficina.Domain.Customers.Enums;
using Oficina.Domain.Inventory.Enums;
using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.Quotes.Pricing;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.Quotes;

public sealed class QuoteQueries(OficinaDbContext context, TimeProvider timeProvider) : IQuoteQueries
{
    public const int LoyalOrders = 3;

    public async Task<EstimateContext?> ContextAsync(Guid serviceOrderId, CancellationToken cancellationToken) =>
        (await ContextsAsync([serviceOrderId], cancellationToken)).GetValueOrDefault(serviceOrderId);

    public async Task<IReadOnlyDictionary<Guid, EstimateContext>> ContextsAsync(IReadOnlyCollection<Guid> serviceOrderIds, CancellationToken cancellationToken)
    {
        var orders = await context.ServiceOrders.AsNoTracking().Where(order => serviceOrderIds.Contains(order.Id)).ToListAsync(cancellationToken);
        var customerIds = orders.Select(order => order.CustomerId).Distinct().ToList();
        var vehicleIds = orders.Select(order => order.VehicleId).Distinct().ToList();
        var customers = await context.Customers.IgnoreQueryFilters().AsNoTracking().Where(customer => customerIds.Contains(customer.Id)).ToDictionaryAsync(customer => customer.Id, cancellationToken);
        var vehicles = await context.Vehicles.IgnoreQueryFilters().AsNoTracking().Where(vehicle => vehicleIds.Contains(vehicle.Id)).ToDictionaryAsync(vehicle => vehicle.Id, cancellationToken);
        var since = timeProvider.GetUtcNow().AddYears(-1);
        var delivered = await context.ServiceOrders.AsNoTracking()
            .Where(order => customerIds.Contains(order.CustomerId) && order.Status == ServiceOrderStatus.Delivered)
            .GroupBy(order => order.CustomerId)
            .Select(group => new { CustomerId = group.Key, Count = group.Count(), Recent = group.Count(order => order.DeliveredAt >= since) })
            .ToDictionaryAsync(row => row.CustomerId, cancellationToken);
        return orders.ToDictionary(order => order.Id, order =>
        {
            var customer = customers[order.CustomerId];
            var vehicle = vehicles[order.VehicleId];
            var history = delivered.GetValueOrDefault(order.CustomerId);
            return new EstimateContext(
                order.Number.Value,
                customer.Id,
                customer.Name,
                customer.Phone.Value,
                customer.Email?.Value,
                customer.ConsentWhatsApp,
                customer.ConsentEmail,
                new CustomerFacts(history?.Recent >= LoyalOrders, history is null, customer.DocumentType == DocumentType.Cnpj),
                vehicle.LicensePlate.Value,
                $"{vehicle.Brand} {vehicle.Model} {vehicle.Year.Value}",
                order.Mileage ?? vehicle.Mileage,
                string.IsNullOrWhiteSpace(order.Diagnosis) ? null : order.Diagnosis);
        });
    }

    public async Task<IReadOnlyList<Estimate>> LatestAsync(string? status, CancellationToken cancellationToken)
    {
        var all = await context.Estimates.AsNoTracking().ToListAsync(cancellationToken);
        return all.GroupBy(estimate => estimate.Number)
            .Select(group => group.MaxBy(estimate => estimate.Version)!)
            .Where(estimate => status is null || estimate.Status == status)
            .OrderByDescending(estimate => estimate.Number)
            .ToList();
    }

    public async Task<IReadOnlyList<Estimate>> VersionsAsync(long number, CancellationToken cancellationToken) =>
        await context.Estimates.AsNoTracking().Where(estimate => estimate.Number == number).OrderByDescending(estimate => estimate.Version).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OpenServiceOrderDto>> OpenServiceOrdersWithoutQuoteAsync(CancellationToken cancellationToken)
    {
        var quoted = context.Estimates.Select(estimate => estimate.ServiceOrderId);
        var rows = await (
            from order in context.ServiceOrders.AsNoTracking()
            where order.Status != ServiceOrderStatus.Delivered && order.Status != ServiceOrderStatus.Completed && !quoted.Contains(order.Id)
            join customer in context.Customers.IgnoreQueryFilters() on order.CustomerId equals customer.Id
            join vehicle in context.Vehicles.IgnoreQueryFilters() on order.VehicleId equals vehicle.Id
            orderby order.CreatedAt descending
            select new { order.Id, order.Number, customer.Name, vehicle.LicensePlate, vehicle.Brand, vehicle.Model, vehicle.Year })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new OpenServiceOrderDto(row.Id, row.Number.Value, row.Name, row.LicensePlate.Value, $"{row.Brand} {row.Model} {row.Year.Value}")).ToList();
    }

    public async Task<IReadOnlyList<QuoteOptionDto>> OptionsAsync(string kind, CancellationToken cancellationToken) => kind switch
    {
        "Package" => (await context.Packages.AsNoTracking().Where(package => package.IsActive).OrderBy(package => package.Name).ToListAsync(cancellationToken))
            .Select(package => new QuoteOptionDto("Package", package.Id, package.Name, package.Items.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), package.Price.Amount, false, null, null)).ToList(),
        "Service" => (await context.Services.AsNoTracking().Where(service => service.IsActive).OrderBy(service => service.Name).ToListAsync(cancellationToken))
            .Select(service => new QuoteOptionDto("Service", service.Id, service.Name, service.EstimatedMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture), service.BasePrice.Amount, false, null, null)).ToList(),
        _ => await PartOptionsAsync(null, cancellationToken),
    };

    public async Task<QuoteOptionDto?> OptionAsync(string kind, Guid itemId, CancellationToken cancellationToken)
    {
        if (kind == "Package")
        {
            var package = await context.Packages.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == itemId && candidate.IsActive, cancellationToken);
            return package is null ? null : new QuoteOptionDto("Package", package.Id, package.Name, null, package.Price.Amount, false, null, null);
        }

        if (kind == "Service")
        {
            var service = await context.Services.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == itemId && candidate.IsActive, cancellationToken);
            return service is null ? null : new QuoteOptionDto("Service", service.Id, service.Name, null, service.BasePrice.Amount, false, null, null);
        }

        return kind == "Part" ? (await PartOptionsAsync(itemId, cancellationToken)).FirstOrDefault() : null;
    }

    private async Task<IReadOnlyList<QuoteOptionDto>> PartOptionsAsync(Guid? itemId, CancellationToken cancellationToken)
    {
        var parts = await context.Parts.AsNoTracking().Where(part => part.IsActive && (itemId == null || part.Id == itemId)).OrderBy(part => part.Name).ToListAsync(cancellationToken);
        var suppliers = await context.Suppliers.AsNoTracking().ToListAsync(cancellationToken);
        return parts.Select(part =>
        {
            var supplier = suppliers.FirstOrDefault(candidate => part.Supplier is not null && string.Equals(candidate.Name, part.Supplier, StringComparison.OrdinalIgnoreCase));
            return new QuoteOptionDto("Part", part.Id, part.Name, part.Code.Value, part.UnitPrice.Amount, part.Type == PartType.Supply,
                new QuoteOptionStockDto(part.StockQuantity, supplier?.LeadTimeDays, part.Supplier), null);
        }).ToList();
    }
}
