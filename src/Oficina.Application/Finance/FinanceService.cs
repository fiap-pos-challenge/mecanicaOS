using System.Globalization;
using Oficina.Application.Analytics;
using Oficina.Application.Common.Numbers;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Dashboard;
using Oficina.Application.Settings;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Finance;

public sealed record FinanceKpisDto(long Received, long Receivable, int ReceivableCount, long Commissions, long AverageTicket, int Orders);

public sealed record PaymentMethodTotalDto(string Method, long Amount, int Count);

public sealed record ReceiptDto(Guid ServiceOrderId, long Number, string CustomerName, DateTimeOffset DeliveredAt, string Method, long Amount);

public sealed record ReceivableDto(Guid ServiceOrderId, long Number, string CustomerName, DateTimeOffset? CompletedAt, long Amount, int DaysWaiting);

public sealed record CommissionDto(string MechanicName, int Orders, long Services, long Commission);

public sealed record FinanceSummaryDto(
    string Period,
    string From,
    string To,
    FinanceKpisDto Kpis,
    IReadOnlyList<PaymentMethodTotalDto> ByMethod,
    IReadOnlyList<ReceiptDto> Receipts,
    IReadOnlyList<ReceivableDto> Receivables,
    IReadOnlyList<CommissionDto> Commissions);

public sealed record DocumentSummaryDto(string Code, string Kind, long Number, string CustomerName, decimal Total, DateTimeOffset IssuedAt);

public sealed record PlatformPlanDto(string Name, DateTimeOffset RenewsAt, int Users, int MaxUsers);

public sealed record PlatformUsageDto(int Customers, int Vehicles, int ServiceOrdersThisMonth, int DocumentsThisMonth, int ActiveAutomations);

public sealed record PlatformModuleDto(string Id, string Name, bool Enabled);

public sealed record PlatformDto(string ShopName, PlatformPlanDto Plan, PlatformUsageDto Usage, IReadOnlyList<PlatformModuleDto> Modules);

public sealed record ReceivableFact(Guid ServiceOrderId, long Number, string CustomerName, DateTimeOffset? CompletedAt, decimal Amount);

public interface IFinanceQueries
{
    Task<IReadOnlyList<ReceivableFact>> ReceivablesAsync(CancellationToken cancellationToken);

    Task<PagedResult<DocumentSummaryDto>> DocumentsAsync(string? kind, string? search, int page, int pageSize, CancellationToken cancellationToken);

    Task<PlatformUsageDto> UsageAsync(DateTimeOffset monthStart, CancellationToken cancellationToken);

    Task<int> ActiveUsersAsync(CancellationToken cancellationToken);
}

public static class FinanceErrors
{
    public static readonly Error InvalidPeriod = new("finance.period", "Período inválido.", ErrorType.Validation,
        [new FieldError("period", "finance.period", "Escolha hoje, semana, mês ou trimestre.")]);

    public static readonly Error InvalidKind = new("documents.kind", "Tipo de documento inválido.", ErrorType.Validation,
        [new FieldError("kind", "documents.kind", "Escolha OS, orçamento ou recibo.")]);
}

public sealed class FinanceService(IAnalyticsQueries analytics, IFinanceQueries queries, SettingsStore settings, TimeProvider timeProvider)
{
    private static readonly IReadOnlyList<PlatformModuleDto> Modules =
    [
        new("serviceOrders", "Ordens de serviço", true),
        new("quotes", "Orçamentos e aprovação", true),
        new("portal", "Portal do cliente", true),
        new("inventory", "Estoque e compras", true),
        new("reports", "Relatórios e assistente", true),
        new("automations", "Automações", true),
        new("finance", "Financeiro", true),
        new("fiscal", "Notas fiscais", false),
    ];

    public async Task<Result<FinanceSummaryDto>> SummaryAsync(string? period, CancellationToken cancellationToken)
    {
        var days = period switch { null or "" or "month" => 30, "today" => 1, "week" => 7, "quarter" => 90, _ => 0 };
        if (days == 0)
        {
            return FinanceErrors.InvalidPeriod;
        }

        var now = timeProvider.GetUtcNow();
        var today = new DateTimeOffset(now.ToOffset(DashboardService.Brazil).Date, DashboardService.Brazil);
        var start = today.AddDays(1 - days);
        var sales = await analytics.SalesAsync(start, now, cancellationToken);
        var receivables = await queries.ReceivablesAsync(cancellationToken);
        var received = sales.Sum(sale => sale.Total);
        var commissions = sales.SelectMany(sale => sale.Services.Select(line => (sale.MechanicName, sale.OrderId, line)))
            .GroupBy(item => item.MechanicName, StringComparer.Ordinal)
            .Select(group => new CommissionDto(group.Key, group.Select(item => item.OrderId).Distinct().Count(), Cents(group.Sum(item => item.line.Total)), Cents(group.Sum(item => item.line.Commission))))
            .OrderByDescending(item => item.Commission).ThenBy(item => item.MechanicName, StringComparer.Ordinal).ToList();
        var kpis = new FinanceKpisDto(
            Cents(received),
            Cents(receivables.Sum(item => item.Amount)),
            receivables.Count,
            commissions.Sum(item => item.Commission),
            sales.Count == 0 ? 0 : Cents(received / sales.Count),
            sales.Count);
        var byMethod = sales.GroupBy(sale => sale.PaymentMethod, StringComparer.Ordinal)
            .Select(group => new PaymentMethodTotalDto(group.Key, Cents(group.Sum(sale => sale.Total)), group.Count()))
            .OrderByDescending(item => item.Amount).ToList();
        var receipts = sales.OrderByDescending(sale => sale.DeliveredAt).Take(50)
            .Select(sale => new ReceiptDto(sale.OrderId, sale.Number, sale.CustomerName, sale.DeliveredAt, sale.PaymentMethod, Cents(sale.Total))).ToList();
        var waiting = receivables.OrderBy(item => item.CompletedAt)
            .Select(item => new ReceivableDto(item.ServiceOrderId, item.Number, item.CustomerName, item.CompletedAt, Cents(item.Amount), item.CompletedAt is { } done ? Math.Max(0, (int)(now - done).TotalDays) : 0)).ToList();
        return new FinanceSummaryDto(period is null or "" ? "month" : period, Iso(start), Iso(today), kpis, byMethod, receipts, waiting, commissions);
    }

    public async Task<Result<PagedResult<DocumentSummaryDto>>> DocumentsAsync(string? kind, string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(kind) && kind is not ("ServiceOrder" or "Quote" or "Receipt"))
        {
            return FinanceErrors.InvalidKind;
        }

        var size = pageSize is < 1 or > PageRequest.MaxPageSize ? PageRequest.DefaultPageSize : pageSize;
        return await queries.DocumentsAsync(string.IsNullOrWhiteSpace(kind) ? null : kind, search?.Trim(), Math.Max(PageRequest.FirstPage, page), size, cancellationToken);
    }

    public async Task<Result<PlatformDto>> PlatformAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var workshop = await settings.GetAsync(SettingsKeys.Workshop, SettingsDefaults.Workshop(now), cancellationToken);
        var local = now.ToOffset(DashboardService.Brazil);
        var monthStart = new DateTimeOffset(local.Year, local.Month, 1, 0, 0, 0, DashboardService.Brazil);
        var users = await queries.ActiveUsersAsync(cancellationToken);
        return new PlatformDto(workshop.Name, new PlatformPlanDto(workshop.Plan.Name, workshop.Plan.RenewsAt, users, workshop.Plan.MaxUsers), await queries.UsageAsync(monthStart, cancellationToken), Modules);
    }

    private static long Cents(decimal value) => (long)Rounding.HalfUp(value * 100, 0);

    private static string Iso(DateTimeOffset value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
