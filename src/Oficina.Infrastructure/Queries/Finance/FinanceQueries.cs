using Microsoft.EntityFrameworkCore;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Finance;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.Finance;

public sealed class FinanceQueries(OficinaDbContext context) : IFinanceQueries
{
    public async Task<IReadOnlyList<ReceivableFact>> ReceivablesAsync(CancellationToken cancellationToken)
    {
        var completed = await context.ServiceOrders.AsNoTracking().Where(order => order.Status == ServiceOrderStatus.Completed).ToListAsync(cancellationToken);
        var ids = completed.Select(order => order.Id).ToList();
        var paid = await context.PortalCharges.AsNoTracking().Where(charge => ids.Contains(charge.ServiceOrderId) && charge.Status == "Paid").Select(charge => charge.ServiceOrderId).ToListAsync(cancellationToken);
        var customerIds = completed.Select(order => order.CustomerId).Distinct().ToList();
        var customers = await context.Customers.AsNoTracking().IgnoreQueryFilters().Where(customer => customerIds.Contains(customer.Id)).Select(customer => new { customer.Id, customer.Name }).ToListAsync(cancellationToken);
        return completed.Where(order => !paid.Contains(order.Id))
            .Select(order => new ReceivableFact(order.Id, order.Number.Value, customers.FirstOrDefault(customer => customer.Id == order.CustomerId)?.Name ?? "Cliente", order.CompletedAt, order.TotalAmount.Amount))
            .ToList();
    }

    public async Task<PagedResult<DocumentSummaryDto>> DocumentsAsync(string? kind, string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.IssuedDocuments.AsNoTracking();
        if (kind is not null)
        {
            query = query.Where(document => document.Kind == kind);
        }

        if (!string.IsNullOrEmpty(search))
        {
            var term = search.ToUpperInvariant();
            query = query.Where(document => document.Code.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(document => document.IssuedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<DocumentSummaryDto>(
            items.Select(document => new DocumentSummaryDto(document.Code, document.Kind, document.Snapshot.Number, document.Snapshot.CustomerName, document.Snapshot.Total, document.IssuedAt)).ToList(),
            page,
            pageSize,
            total);
    }

    public async Task<PlatformUsageDto> UsageAsync(DateTimeOffset monthStart, CancellationToken cancellationToken) => new(
        await context.Customers.CountAsync(cancellationToken),
        await context.Vehicles.CountAsync(cancellationToken),
        await context.ServiceOrders.CountAsync(order => order.CreatedAt >= monthStart, cancellationToken),
        await context.IssuedDocuments.CountAsync(document => document.IssuedAt >= monthStart, cancellationToken),
        await context.AutomationRules.CountAsync(rule => rule.Enabled, cancellationToken));

    public Task<int> ActiveUsersAsync(CancellationToken cancellationToken) => context.Users.CountAsync(user => user.IsActive, cancellationToken);
}
