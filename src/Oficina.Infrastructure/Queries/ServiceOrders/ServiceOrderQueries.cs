using Microsoft.EntityFrameworkCore;
using Oficina.Application.Common.Pagination;
using Oficina.Application.ServiceOrders;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.ValueObjects;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.ServiceOrders;

public sealed class ServiceOrderQueries(OficinaDbContext context, TimeProvider timeProvider) : IServiceOrderQueries, IServiceOrderBoardQueries
{
    public const string LateTag = "Late";
    public const string MissingPartTag = "MissingPart";
    public const string ReminderSentTag = "ReminderSent";
    public const int SummaryMaxLength = 80;

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

        if (plate is not null)
        {
            var vehicleIds = context.Vehicles.IgnoreQueryFilters().Where(vehicle => vehicle.LicensePlate == plate).Select(vehicle => vehicle.Id);
            orders = orders.Where(order => vehicleIds.Contains(order.VehicleId));
        }

        var totalItems = await orders.CountAsync(cancellationToken);
        var items = await orders
            .Include(order => order.ServiceItems)
            .Include(order => order.PartItems)
            .Include(order => order.Quotes)
            .AsSplitQuery()
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        var lookup = await LoadContextAsync(items, cancellationToken);
        return new PagedResult<ServiceOrderSummaryDto>(items.Select(order => Summary(order, lookup)).ToList(), page.Page, page.PageSize, totalItems);
    }

    public async Task<ServiceOrderDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await WithItems().SingleOrDefaultAsync(order => order.Id == id, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var lookup = await LoadContextAsync([order], cancellationToken);
        var photos = await PhotosAsync([order.Id], cancellationToken);
        var customer = await context.Customers.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(customer => customer.Id == order.CustomerId, cancellationToken);
        var vehicle = lookup.Vehicles[order.VehicleId];
        var card = Summary(order, lookup);
        var partUnits = await context.Parts.IgnoreQueryFilters().AsNoTracking()
            .Where(part => order.PartItems.Select(item => item.PartId).Contains(part.Id))
            .ToDictionaryAsync(part => part.Id, part => (part.UnitOfMeasure, part.StockQuantity), cancellationToken);

        return new ServiceOrderDetailDto(
            order.Id,
            order.Number.Value,
            order.Status,
            order.Version,
            card.LicensePlate,
            card.CustomerName,
            card.Vehicle,
            card.Summary,
            card.MechanicName,
            order.PromisedAt,
            card.HasQuoteItems,
            card.Tags,
            card.UpdatedAt,
            order.ProblemDescription,
            order.Mileage,
            order.TotalAmount.Amount,
            new ServiceOrderCustomerDto(customer.Id, customer.Name, customer.Document.Value, customer.Email?.Value),
            new ServiceOrderVehicleDto(vehicle.Id, vehicle.LicensePlate.Value, vehicle.Brand, vehicle.Model, vehicle.Year.Value),
            ServiceItems(order),
            order.PartItems.Select(item =>
            {
                var (unit, stock) = partUnits.GetValueOrDefault(item.PartId, ("un", 0m));
                var reserved = order.Status is ServiceOrderStatus.InProgress or ServiceOrderStatus.Completed or ServiceOrderStatus.Delivered;
                return new ServiceOrderBoardPartDto(item.Id, item.PartId, item.Description, item.Quantity.Value, unit, item.UnitPrice.Amount, item.Total.Amount, reserved, reserved || stock >= item.Quantity.Value ? null : DefaultLeadTimeDays);
            }).ToList(),
            BoardQuote(order),
            order.StatusHistory
                .OrderBy(history => history.ChangedAt)
                .Select(history => new ServiceOrderStatusHistoryDto(
                    history.PreviousStatus,
                    history.NewStatus,
                    history.ChangedByUserId,
                    history.ChangedByUserId is { } userId ? lookup.Users.GetValueOrDefault(userId) : null,
                    history.Note,
                    history.ChangedAt))
                .ToList(),
            photos.GetValueOrDefault(order.Id) ?? [],
            order.TimeEntries
                .OrderBy(entry => entry.StartedAt)
                .Select(entry => new ServiceOrderTimeEntryDto(
                    entry.Id,
                    entry.MechanicId,
                    lookup.Users.GetValueOrDefault(entry.MechanicId) ?? string.Empty,
                    entry.StartedAt,
                    entry.EndedAt,
                    entry.EndedAt is null ? (int)((timeProvider.GetUtcNow() - entry.StartedAt).TotalMinutes) : entry.Minutes))
                .ToList(),
            (await context.IssuedDocuments.AsNoTracking().Where(document => document.ServiceOrderId == order.Id).OrderBy(document => document.IssuedAt).ToListAsync(cancellationToken))
                .Select(document => new ServiceOrderDocumentDto(document.Kind, document.Code, document.IssuedAt)).ToList(),
            order.CreatedAt,
            order.ExecutionStartedAt,
            order.CompletedAt,
            order.DeliveredAt);
    }

    public async Task<MechanicOrderDto?> GetWorkAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await WorkQuery().SingleOrDefaultAsync(order => order.Id == id, cancellationToken);
        return order is null ? null : (await WorkAsync([order], cancellationToken))[0];
    }

    public async Task<IReadOnlyList<MechanicOrderDto>> ListMineAsync(Guid mechanicId, CancellationToken cancellationToken)
    {
        var orders = await WorkQuery()
            .Where(order => order.Status != ServiceOrderStatus.Completed && order.Status != ServiceOrderStatus.Delivered
                && (order.MechanicId == mechanicId || (order.MechanicId == null && order.Status != ServiceOrderStatus.WaitingApproval)))
            .OrderBy(order => order.PromisedAt == null)
            .ThenBy(order => order.PromisedAt)
            .ThenBy(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
        return await WorkAsync(orders, cancellationToken);
    }

    public async Task<IReadOnlyList<SuggestedItemDto>> SuggestAsync(string description, CancellationToken cancellationToken)
    {
        var rules = SuggestionRules.Where(rule => rule.Keywords.Any(keyword => description.Contains(keyword, StringComparison.OrdinalIgnoreCase))).ToList();
        if (rules.Count == 0)
        {
            return [];
        }

        var services = await context.Services.AsNoTracking().Where(service => service.IsActive).Select(service => new { service.Id, service.Name }).ToListAsync(cancellationToken);
        var parts = await context.Parts.AsNoTracking().Where(part => part.IsActive).Select(part => new { part.Id, part.Name }).ToListAsync(cancellationToken);
        var suggestions = new List<SuggestedItemDto>();
        foreach (var rule in rules)
        {
            var match = rule.Kind == "Service"
                ? services.FirstOrDefault(service => service.Name.Contains(rule.Target, StringComparison.OrdinalIgnoreCase)) is { } service ? (service.Id, service.Name) : default((Guid, string)?)
                : parts.FirstOrDefault(part => part.Name.Contains(rule.Target, StringComparison.OrdinalIgnoreCase)) is { } part ? (part.Id, part.Name) : default((Guid, string)?);
            if (match is { } found && suggestions.TrueForAll(item => item.ItemId != found.Item1))
            {
                suggestions.Add(new SuggestedItemDto(rule.Kind, found.Item1, found.Item2, rule.Confidence, rule.Reason));
            }
        }

        return suggestions;
    }

    private const int DefaultLeadTimeDays = 3;

    private static readonly (string[] Keywords, string Kind, string Target, decimal Confidence, string Reason)[] SuggestionRules =
    [
        (["bieleta", "suspens", "amortec", "coxim"], "Service", "Alinhamento", 0.82m, "Folga na suspensão altera a geometria."),
        (["freio", "pastilha", "disco"], "Part", "Pastilha", 0.91m, "Desgaste citado na descrição."),
        (["óleo", "oleo", "vazamento"], "Service", "óleo", 0.77m, "Nível ou vazamento de óleo."),
        (["vela", "falha", "ignição", "ignicao"], "Part", "Vela", 0.86m, "Falha de ignição."),
    ];

    private sealed record LookupContext(
        IReadOnlyDictionary<Guid, Domain.Customers.Entities.Vehicle> Vehicles,
        IReadOnlyDictionary<Guid, string> Customers,
        IReadOnlyDictionary<Guid, string> Users,
        IReadOnlyDictionary<Guid, decimal> Stock,
        DateTimeOffset Now);

    private async Task<LookupContext> LoadContextAsync(IReadOnlyList<ServiceOrder> orders, CancellationToken cancellationToken)
    {
        var vehicleIds = orders.Select(order => order.VehicleId).Distinct().ToList();
        var customerIds = orders.Select(order => order.CustomerId).Distinct().ToList();
        var userIds = orders.SelectMany(order => order.StatusHistory.Select(history => history.ChangedByUserId).OfType<Guid>()
                .Concat(order.TimeEntries.Select(entry => entry.MechanicId))
                .Concat(order.MechanicId is { } mechanic ? [mechanic] : []))
            .Distinct()
            .ToList();
        var partIds = orders.SelectMany(order => order.PartItems.Select(item => item.PartId)).Distinct().ToList();
        return new LookupContext(
            await context.Vehicles.IgnoreQueryFilters().AsNoTracking().Where(vehicle => vehicleIds.Contains(vehicle.Id)).ToDictionaryAsync(vehicle => vehicle.Id, cancellationToken),
            await context.Customers.IgnoreQueryFilters().AsNoTracking().Where(customer => customerIds.Contains(customer.Id)).ToDictionaryAsync(customer => customer.Id, customer => customer.Name, cancellationToken),
            await context.Users.IgnoreQueryFilters().AsNoTracking().Where(user => userIds.Contains(user.Id)).ToDictionaryAsync(user => user.Id, user => user.Name, cancellationToken),
            await context.Parts.IgnoreQueryFilters().AsNoTracking().Where(part => partIds.Contains(part.Id)).ToDictionaryAsync(part => part.Id, part => part.StockQuantity, cancellationToken),
            timeProvider.GetUtcNow());
    }

    private static ServiceOrderSummaryDto Summary(ServiceOrder order, LookupContext lookup)
    {
        var vehicle = lookup.Vehicles[order.VehicleId];
        var tags = new List<string>();
        if (order.PromisedAt < lookup.Now && order.Status is not (ServiceOrderStatus.Completed or ServiceOrderStatus.Delivered))
        {
            tags.Add(LateTag);
        }

        if (order.Status is ServiceOrderStatus.InDiagnosis or ServiceOrderStatus.WaitingApproval
            && order.PartItems.GroupBy(item => item.PartId).Any(group => lookup.Stock.GetValueOrDefault(group.Key) < group.Sum(item => item.Quantity.Value)))
        {
            tags.Add(MissingPartTag);
        }

        if (order.Status == ServiceOrderStatus.WaitingApproval && order.CurrentQuote is { } quote && lookup.Now - quote.SentAt > TimeSpan.FromHours(24))
        {
            tags.Add(ReminderSentTag);
        }

        var summary = order.ProblemDescription.Length > SummaryMaxLength ? order.ProblemDescription[..SummaryMaxLength] : order.ProblemDescription;
        return new ServiceOrderSummaryDto(
            order.Id,
            order.Number.Value,
            order.Status,
            order.Version,
            vehicle.LicensePlate.Value,
            lookup.Customers.GetValueOrDefault(order.CustomerId) ?? string.Empty,
            $"{vehicle.Brand} {vehicle.Model} {vehicle.Year.Value}",
            summary,
            order.MechanicId is { } mechanicId ? lookup.Users.GetValueOrDefault(mechanicId) : null,
            order.PromisedAt,
            order.ServiceItems.Count + order.PartItems.Count > 0,
            tags,
            order.CreatedAt,
            order.UpdatedAt ?? order.CreatedAt,
            order.TotalAmount.Amount);
    }

    private static ServiceOrderBoardQuoteDto? BoardQuote(ServiceOrder order) =>
        order.CurrentQuote is { } quote
            ? new ServiceOrderBoardQuoteDto(
                quote.Id,
                order.Number.Value,
                quote.Version,
                quote.Status,
                quote.Total.Amount,
                quote.ServicesTotal.Amount,
                quote.PartsTotal.Amount,
                quote.SentAt,
                quote.RespondedAt,
                quote.RejectionReason,
                order.ServiceItems.Select(item => new ServiceOrderBoardQuoteItemDto(item.Description, item.Quantity, item.Total.Amount))
                    .Concat(order.PartItems.Select(item => new ServiceOrderBoardQuoteItemDto(item.Description, item.Quantity.Value, item.Total.Amount)))
                    .ToList())
            : null;

    private async Task<Dictionary<Guid, IReadOnlyList<ServiceOrderPhotoDto>>> PhotosAsync(IReadOnlyCollection<Guid> orderIds, CancellationToken cancellationToken) =>
        (await context.ServiceOrderPhotos.AsNoTracking()
            .Where(photo => orderIds.Contains(photo.ServiceOrderId))
            .OrderBy(photo => photo.TakenAt)
            .Select(photo => new { photo.Id, photo.ServiceOrderId, photo.Caption, photo.TakenAt })
            .ToListAsync(cancellationToken))
            .GroupBy(photo => photo.ServiceOrderId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<ServiceOrderPhotoDto>)group.Select(photo => new ServiceOrderPhotoDto(photo.Id, ServiceOrderPhotoPaths.Admin(photo.ServiceOrderId, photo.Id), photo.Caption, photo.TakenAt)).ToList());

    private IQueryable<ServiceOrder> WorkQuery() =>
        context.ServiceOrders.AsNoTracking()
            .Include(order => order.TimeEntries)
            .AsSplitQuery();

    private async Task<IReadOnlyList<MechanicOrderDto>> WorkAsync(IReadOnlyList<ServiceOrder> orders, CancellationToken cancellationToken)
    {
        var vehicleIds = orders.Select(order => order.VehicleId).Distinct().ToList();
        var vehicles = await context.Vehicles.IgnoreQueryFilters().AsNoTracking().Where(vehicle => vehicleIds.Contains(vehicle.Id)).ToDictionaryAsync(vehicle => vehicle.Id, cancellationToken);
        var photos = await PhotosAsync(orders.Select(order => order.Id).ToList(), cancellationToken);
        return orders.Select(order =>
        {
            var vehicle = vehicles[order.VehicleId];
            return new MechanicOrderDto(
                order.Id,
                order.Number.Value,
                order.Status,
                $"{vehicle.Brand} {vehicle.Model} {vehicle.Year.Value}",
                vehicle.LicensePlate.Value,
                order.ProblemDescription,
                order.PromisedAt,
                order.Checklist.Select(item => new ChecklistItemDto(item.Id, item.Label, item.State)).ToList(),
                photos.GetValueOrDefault(order.Id) ?? [],
                order.Diagnosis,
                order.SentToQuote,
                order.TimerRunning,
                order.Version);
        }).ToList();
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
            .Include(order => order.TimeEntries)
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
