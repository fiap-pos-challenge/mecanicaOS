using Microsoft.EntityFrameworkCore;
using Oficina.Application.Common.Pagination;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.ValueObjects;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.ServiceOrders;

public sealed class ServiceOrderQueries(OficinaDbContext context) : IServiceOrderQueries
{
    public async Task<PagedResult<ServiceOrderSummaryDto>> ListAsync(
        PageRequest page,
        ServiceOrderStatus? status,
        LicensePlate? plate,
        CancellationToken cancellationToken)
    {
        var orders = context.ServiceOrders.AsNoTracking();
        if (status is not null)
        {
            orders = orders.Where(order => order.Status == status);
        }

        var vehicles = context.Vehicles.IgnoreQueryFilters().AsNoTracking();
        if (plate is not null)
        {
            vehicles = vehicles.Where(vehicle => vehicle.LicensePlate == plate);
        }

        var rows =
            from order in orders
            join vehicle in vehicles on order.VehicleId equals vehicle.Id
            join customer in context.Customers.IgnoreQueryFilters().AsNoTracking() on order.CustomerId equals customer.Id
            select new { order.Id, order.Number, order.Status, vehicle.LicensePlate, CustomerName = customer.Name, order.CreatedAt, order.TotalAmount };

        var totalItems = await rows.CountAsync(cancellationToken);
        var items = await rows
            .OrderByDescending(row => row.CreatedAt)
            .ThenByDescending(row => row.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        var summaries = items
            .Select(row => new ServiceOrderSummaryDto(
                row.Id,
                row.Number.Value,
                row.Status,
                row.LicensePlate.Value,
                row.CustomerName,
                row.CreatedAt,
                row.TotalAmount.Amount))
            .ToList();

        return new PagedResult<ServiceOrderSummaryDto>(summaries, page.Page, page.PageSize, totalItems);
    }

    public async Task<ServiceOrderDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await WithItems().SingleOrDefaultAsync(order => order.Id == id, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var customer = await context.Customers.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(customer => customer.Id == order.CustomerId, cancellationToken);
        var vehicle = await LoadVehicleAsync(order, cancellationToken);

        var userIds = order.StatusHistory
            .Select(history => history.ChangedByUserId)
            .OfType<Guid>()
            .Distinct()
            .ToList();
        var userNames = await context.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.Name, cancellationToken);

        return new ServiceOrderDetailDto(
            order.Id,
            order.Number.Value,
            order.Status,
            order.ProblemDescription,
            order.TotalAmount.Amount,
            new ServiceOrderCustomerDto(customer.Id, customer.Name, customer.Document.Value, customer.Email?.Value),
            vehicle,
            ServiceItems(order),
            PartItems(order),
            Quote(order),
            order.StatusHistory
                .OrderBy(history => history.ChangedAt)
                .Select(history => new ServiceOrderStatusHistoryDto(
                    history.PreviousStatus,
                    history.NewStatus,
                    history.ChangedByUserId,
                    history.ChangedByUserId is { } userId ? userNames.GetValueOrDefault(userId) : null,
                    history.Note,
                    history.ChangedAt))
                .ToList(),
            order.CreatedAt,
            order.ExecutionStartedAt,
            order.CompletedAt,
            order.DeliveredAt);
    }

    public async Task<PublicServiceOrderDto?> GetPublicByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await WithItems().SingleOrDefaultAsync(order => order.Id == id, cancellationToken);
        return order is null ? null : await ToPublicAsync(order, cancellationToken);
    }

    public async Task<PublicServiceOrderDto?> LookupAsync(ServiceOrderNumber number, Document document, CancellationToken cancellationToken)
    {
        var customerIds = context.Customers.IgnoreQueryFilters()
            .Where(customer => customer.Document == document)
            .Select(customer => customer.Id);
        var order = await WithItems()
            .SingleOrDefaultAsync(order => order.Number == number && customerIds.Contains(order.CustomerId), cancellationToken);
        return order is null ? null : await ToPublicAsync(order, cancellationToken);
    }

    public async Task<AverageExecutionTimeDto> GetAverageExecutionTimeAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var orders = context.ServiceOrders.AsNoTracking()
            .Where(order => order.Status == ServiceOrderStatus.Completed || order.Status == ServiceOrderStatus.Delivered);
        if (from is { } start)
        {
            var startAt = new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            orders = orders.Where(order => order.CompletedAt >= startAt);
        }

        if (to is { } end)
        {
            var endBefore = new DateTimeOffset(end.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            orders = orders.Where(order => order.CompletedAt < endBefore);
        }

        var periods = await orders
            .Select(order => new { order.ExecutionStartedAt, order.CompletedAt })
            .ToListAsync(cancellationToken);
        if (periods.Count == 0)
        {
            return new AverageExecutionTimeDto(null, 0);
        }

        var averageTicks = periods.Average(period => (period.CompletedAt - period.ExecutionStartedAt).GetValueOrDefault().Ticks);
        return new AverageExecutionTimeDto((long)TimeSpan.FromTicks((long)averageTicks).TotalMinutes, periods.Count);
    }

    private static List<ServiceOrderServiceItemDto> ServiceItems(ServiceOrder order) =>
        order.ServiceItems
            .Select(item => new ServiceOrderServiceItemDto(item.Id, item.ServiceId, item.Description, item.Quantity, item.UnitPrice.Amount, item.Total.Amount))
            .ToList();

    private static List<ServiceOrderPartItemDto> PartItems(ServiceOrder order) =>
        order.PartItems
            .Select(item => new ServiceOrderPartItemDto(item.Id, item.PartId, item.Description, item.Quantity.Value, item.UnitPrice.Amount, item.Total.Amount))
            .ToList();

    private static ServiceOrderQuoteDto? Quote(ServiceOrder order) =>
        order.CurrentQuote is { } quote
            ? new ServiceOrderQuoteDto(
                quote.ServicesTotal.Amount,
                quote.PartsTotal.Amount,
                quote.Total.Amount,
                quote.Status,
                quote.SentAt,
                quote.RespondedAt,
                quote.RejectionReason)
            : null;

    private IQueryable<ServiceOrder> WithItems() =>
        context.ServiceOrders.AsNoTracking()
            .Include(order => order.ServiceItems)
            .Include(order => order.PartItems)
            .Include(order => order.Quotes)
            .Include(order => order.StatusHistory)
            .AsSplitQuery();

    private async Task<ServiceOrderVehicleDto> LoadVehicleAsync(ServiceOrder order, CancellationToken cancellationToken)
    {
        var vehicle = await context.Vehicles.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(vehicle => vehicle.Id == order.VehicleId, cancellationToken);
        return new ServiceOrderVehicleDto(vehicle.Id, vehicle.LicensePlate.Value, vehicle.Brand, vehicle.Model, vehicle.Year.Value);
    }

    private async Task<PublicServiceOrderDto> ToPublicAsync(ServiceOrder order, CancellationToken cancellationToken) =>
        new(
            order.Number.Value,
            order.Status,
            order.ProblemDescription,
            order.TotalAmount.Amount,
            await LoadVehicleAsync(order, cancellationToken),
            ServiceItems(order),
            PartItems(order),
            Quote(order),
            order.CreatedAt,
            order.ExecutionStartedAt,
            order.CompletedAt,
            order.DeliveredAt);
}
