using Microsoft.EntityFrameworkCore;
using Oficina.Application.Common.Numbers;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Inventory.Dtos;
using Oficina.Application.Inventory.Queries;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Enums;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.Inventory;

public sealed class InventoryQueries(OficinaDbContext context, TimeProvider timeProvider) : IInventoryQueries
{
    public const int TurnoverGoalDays = 30;
    public const int IdleDays = 90;
    public const string SystemUser = "Sistema";

    private sealed record Flow(decimal Outflow30d, decimal OutflowPrevious, DateTimeOffset? LastOutflowAt);

    public async Task<StockOverviewDto> OverviewAsync(PageRequest page, string? term, string? situation, string? location, CancellationToken cancellationToken)
    {
        var parts = await context.Parts.AsNoTracking().Where(part => part.IsActive).ToListAsync(cancellationToken);
        var flows = await FlowsAsync(parts.Select(part => part.Id).ToList(), cancellationToken);
        var now = timeProvider.GetUtcNow();
        var items = parts.Select(part => Item(part, flows.GetValueOrDefault(part.Id), now)).ToList();
        var filtered = items
            .Where(item => string.IsNullOrWhiteSpace(term) || item.Name.Contains(term.Trim(), StringComparison.OrdinalIgnoreCase) || item.Code.Contains(term.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(item => situation is null || item.Situation == situation)
            .Where(item => string.IsNullOrWhiteSpace(location) || item.Location == location)
            .OrderBy(item => item.Code, StringComparer.Ordinal)
            .ToList();
        var below = parts.Where(part => part.StockQuantity < part.MinimumStock).ToList();
        var idle = items.Where(item => item.Situation == "Idle").ToList();
        var outflow = flows.Values.Sum(flow => flow.Outflow30d);
        var previous = flows.Values.Sum(flow => flow.OutflowPrevious);
        var stockValue = parts.Sum(part => part.StockQuantity * part.Cost.Amount);
        var monthlyCost = parts.Sum(part => flows.GetValueOrDefault(part.Id)?.Outflow30d * part.Cost.Amount ?? 0);
        var kpis = new StockKpisDto(
            below.Count,
            Rounding.HalfUp(below.Sum(part => Suggested(part, flows.GetValueOrDefault(part.Id)) * part.Cost.Amount), 2),
            outflow,
            previous == 0 ? null : Rounding.HalfUp((outflow - previous) / previous * 100, 0),
            monthlyCost == 0 ? null : Rounding.HalfUp(stockValue / monthlyCost * 30, 0),
            TurnoverGoalDays,
            idle.Count,
            Rounding.HalfUp(idle.Sum(item => item.StockQuantity * item.UnitCost), 2),
            Rounding.HalfUp(stockValue, 2),
            parts.Count);
        return new StockOverviewDto(
            kpis,
            filtered.Skip(page.Skip).Take(page.PageSize).ToList(),
            page.Page,
            page.PageSize,
            filtered.Count,
            parts.Select(part => part.Location).Distinct().Order(StringComparer.Ordinal).ToList());
    }

    public async Task<StockMovementListDto?> MovementsAsync(Guid partId, CancellationToken cancellationToken)
    {
        var part = await context.Parts.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == partId, cancellationToken);
        if (part is null)
        {
            return null;
        }

        var flows = await FlowsAsync([part.Id], cancellationToken);
        var movements = await MovementQuery().Where(movement => movement.PartId == partId).OrderByDescending(movement => movement.CreatedAt).ToListAsync(cancellationToken);
        var names = await NamesAsync(movements.Select(movement => movement.UserId), cancellationToken);
        return new StockMovementListDto(Item(part, flows.GetValueOrDefault(part.Id), timeProvider.GetUtcNow()), movements.Select(movement => Movement(movement, names)).ToList());
    }

    public async Task<StockMovementDto?> MovementAsync(Guid movementId, CancellationToken cancellationToken)
    {
        var movement = await MovementQuery().SingleOrDefaultAsync(candidate => candidate.Id == movementId, cancellationToken);
        return movement is null ? null : Movement(movement, await NamesAsync([movement.UserId], cancellationToken));
    }

    public async Task<StockItemDto?> ByBarcodeAsync(string barcode, CancellationToken cancellationToken)
    {
        var part = await context.Parts.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Barcode == barcode, cancellationToken);
        return part is null ? null : Item(part, (await FlowsAsync([part.Id], cancellationToken)).GetValueOrDefault(part.Id), timeProvider.GetUtcNow());
    }

    public async Task<StockCountListDto> CountsAsync(CancellationToken cancellationToken)
    {
        var counts = await context.StockCounts.AsNoTracking().OrderByDescending(count => count.OpenedAt).ToListAsync(cancellationToken);
        return new StockCountListDto(await CountDtosAsync(counts, cancellationToken));
    }

    public async Task<StockCountDto?> CountAsync(Guid id, CancellationToken cancellationToken)
    {
        var count = await context.StockCounts.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return count is null ? null : (await CountDtosAsync([count], cancellationToken))[0];
    }

    public async Task<SupplierListDto> SuppliersAsync(CancellationToken cancellationToken) =>
        new(await context.Suppliers.AsNoTracking().OrderBy(supplier => supplier.Name)
            .Select(supplier => new SupplierDto(supplier.Id, supplier.Name, supplier.Document, supplier.Email, supplier.Phone, supplier.LeadTimeDays, supplier.IsActive))
            .ToListAsync(cancellationToken));

    public async Task<PurchaseSuggestionListDto> SuggestionsAsync(CancellationToken cancellationToken)
    {
        var parts = await context.Parts.AsNoTracking().Where(part => part.IsActive && part.StockQuantity < part.MinimumStock).ToListAsync(cancellationToken);
        var flows = await FlowsAsync(parts.Select(part => part.Id).ToList(), cancellationToken);
        var suppliers = await context.Suppliers.AsNoTracking().ToListAsync(cancellationToken);
        return new PurchaseSuggestionListDto(parts
            .Select(part => (Part: part, Suggested: Suggested(part, flows.GetValueOrDefault(part.Id))))
            .Where(row => row.Suggested > 0)
            .OrderBy(row => row.Part.Code.Value, StringComparer.Ordinal)
            .Select(row => new PurchaseSuggestionDto(
                row.Part.Id,
                row.Part.Code.Value,
                row.Part.Name,
                row.Part.Type.ToString(),
                row.Part.UnitOfMeasure,
                suppliers.FirstOrDefault(supplier => row.Part.Supplier is not null && string.Equals(supplier.Name, row.Part.Supplier, StringComparison.OrdinalIgnoreCase))?.Id,
                row.Part.StockQuantity,
                row.Part.MinimumStock,
                flows.GetValueOrDefault(row.Part.Id)?.Outflow30d ?? 0,
                row.Suggested,
                row.Part.Cost.Amount))
            .ToList());
    }

    public async Task<PurchaseOrderListDto> OrdersAsync(CancellationToken cancellationToken)
    {
        var orders = await context.PurchaseOrders.AsNoTracking().OrderByDescending(order => order.Number).ToListAsync(cancellationToken);
        return new PurchaseOrderListDto(await OrderDtosAsync(orders, cancellationToken));
    }

    public async Task<PurchaseOrderDto?> OrderAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await context.PurchaseOrders.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return order is null ? null : (await OrderDtosAsync([order], cancellationToken))[0];
    }

    private IQueryable<StockMovement> MovementQuery() => context.StockMovements.AsNoTracking();

    private async Task<Dictionary<Guid, Flow>> FlowsAsync(IReadOnlyCollection<Guid> partIds, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var from = now.AddDays(-60);
        var outflowTypes = new[] { StockMovementType.Deduction, StockMovementType.Exit };
        var recent = await context.StockMovements.AsNoTracking()
            .Where(movement => partIds.Contains(movement.PartId) && outflowTypes.Contains(movement.Type) && movement.CreatedAt >= from)
            .Select(movement => new { movement.PartId, movement.Quantity, movement.CreatedAt })
            .ToListAsync(cancellationToken);
        var last = await context.StockMovements.AsNoTracking()
            .Where(movement => partIds.Contains(movement.PartId) && outflowTypes.Contains(movement.Type))
            .GroupBy(movement => movement.PartId)
            .Select(group => new { PartId = group.Key, Last = group.Max(movement => movement.CreatedAt) })
            .ToDictionaryAsync(row => row.PartId, row => row.Last, cancellationToken);
        var middle = now.AddDays(-30);
        return partIds.ToDictionary(
            id => id,
            id => new Flow(
                recent.Where(row => row.PartId == id && row.CreatedAt >= middle).Sum(row => row.Quantity.Value),
                recent.Where(row => row.PartId == id && row.CreatedAt < middle).Sum(row => row.Quantity.Value),
                last.TryGetValue(id, out var at) ? at : null));
    }

    private static StockItemDto Item(Part part, Flow? flow, DateTimeOffset now)
    {
        var situation = part.StockQuantity <= 0 ? "OutOfStock"
            : part.StockQuantity < part.MinimumStock ? "Reorder"
            : flow?.LastOutflowAt is not { } last || now - last > TimeSpan.FromDays(IdleDays) ? "Idle"
            : "Ok";
        return new StockItemDto(part.Id, part.Code.Value, part.Name, part.Type.ToString(), part.UnitOfMeasure, part.Location, part.StockQuantity, part.MinimumStock,
            flow?.Outflow30d ?? 0, part.Cost.Amount, flow?.LastOutflowAt, part.Barcode, situation);
    }

    private static decimal Suggested(Part part, Flow? flow)
    {
        var target = Math.Max(part.MinimumStock * 2, (flow?.Outflow30d ?? 0) + part.MinimumStock);
        var missing = target - part.StockQuantity;
        if (missing <= 0 || part.StockQuantity >= part.MinimumStock)
        {
            return 0;
        }

        return part.Type == PartType.Part ? decimal.Ceiling(missing) : decimal.Ceiling(missing * 10) / 10;
    }

    private static StockMovementDto Movement(StockMovement movement, IReadOnlyDictionary<Guid, string> names) =>
        new(
            movement.Id,
            movement.Type switch { StockMovementType.Entry => "In", StockMovementType.Adjustment => "Adjustment", _ => "Out" },
            movement.Type == StockMovementType.Adjustment ? movement.SignedQuantity : movement.Quantity.Value,
            movement.BalanceAfter,
            movement.Reason.StartsWith("serviceOrder:", StringComparison.Ordinal) ? "Aplicada na OS" : movement.Reason,
            movement.UserId is { } userId ? names.GetValueOrDefault(userId) ?? SystemUser : SystemUser,
            movement.ServiceOrderNumber,
            movement.CreatedAt);

    private async Task<Dictionary<Guid, string>> NamesAsync(IEnumerable<Guid?> ids, CancellationToken cancellationToken)
    {
        var list = ids.OfType<Guid>().Distinct().ToList();
        return await context.Users.IgnoreQueryFilters().AsNoTracking().Where(user => list.Contains(user.Id)).ToDictionaryAsync(user => user.Id, user => user.Name, cancellationToken);
    }

    private async Task<IReadOnlyList<StockCountDto>> CountDtosAsync(IReadOnlyList<StockCount> counts, CancellationToken cancellationToken)
    {
        var partIds = counts.SelectMany(count => count.Lines.Select(line => line.PartId)).Distinct().ToList();
        var parts = await context.Parts.IgnoreQueryFilters().AsNoTracking().Where(part => partIds.Contains(part.Id)).ToDictionaryAsync(part => part.Id, cancellationToken);
        var names = await NamesAsync(counts.SelectMany(count => new[] { count.OpenedBy, count.DecidedBy }), cancellationToken);
        return counts.Select(count => new StockCountDto(
            count.Id,
            count.Location,
            count.Status.ToString(),
            count.OpenedBy is { } opened ? names.GetValueOrDefault(opened) ?? SystemUser : SystemUser,
            count.OpenedAt,
            count.SubmittedAt,
            count.DecidedBy is { } decided ? names.GetValueOrDefault(decided) : null,
            count.DecidedAt,
            count.RejectionReason,
            count.Lines.Select(line =>
            {
                var part = parts.GetValueOrDefault(line.PartId);
                return new CountLineDto(line.PartId, part?.Code.Value ?? string.Empty, part?.Name ?? string.Empty, part?.Type.ToString() ?? "Part", part?.UnitOfMeasure ?? "un", line.SystemQuantity, line.CountedQuantity);
            }).ToList())).ToList();
    }

    private async Task<IReadOnlyList<PurchaseOrderDto>> OrderDtosAsync(IReadOnlyList<PurchaseOrder> orders, CancellationToken cancellationToken)
    {
        var supplierIds = orders.Select(order => order.SupplierId).Distinct().ToList();
        var suppliers = await context.Suppliers.IgnoreQueryFilters().AsNoTracking().Where(supplier => supplierIds.Contains(supplier.Id)).ToDictionaryAsync(supplier => supplier.Id, supplier => supplier.Name, cancellationToken);
        var partIds = orders.SelectMany(order => order.Lines.Select(line => line.PartId)).Distinct().ToList();
        var parts = await context.Parts.IgnoreQueryFilters().AsNoTracking().Where(part => partIds.Contains(part.Id)).ToDictionaryAsync(part => part.Id, cancellationToken);
        return orders.Select(order => new PurchaseOrderDto(
            order.Id,
            order.Number,
            order.SupplierId,
            suppliers.GetValueOrDefault(order.SupplierId) ?? string.Empty,
            order.Status.ToString(),
            Rounding.HalfUp(order.Total, 2),
            order.CreatedAt,
            order.SentAt,
            order.Lines.Select(line => new PurchaseOrderLineDto(line.PartId, parts.GetValueOrDefault(line.PartId)?.Code.Value ?? string.Empty, parts.GetValueOrDefault(line.PartId)?.Name ?? string.Empty, line.Quantity, line.UnitCost)).ToList())).ToList();
    }
}
