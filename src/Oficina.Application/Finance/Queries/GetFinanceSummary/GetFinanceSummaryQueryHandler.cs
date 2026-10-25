using System.Globalization;
using MediatR;
using Oficina.Application.Analytics;
using Oficina.Application.Dashboard;
using Oficina.Application.Finance.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.Application.Finance.Queries.GetFinanceSummary;

public sealed class GetFinanceSummaryQueryHandler(IAnalyticsQueries analytics, IFinanceQueries queries, TimeProvider timeProvider)
    : IRequestHandler<GetFinanceSummaryQuery, Result<FinanceSummaryDto>>
{
    public const int ReceiptLimit = 50;

    public async Task<Result<FinanceSummaryDto>> Handle(GetFinanceSummaryQuery query, CancellationToken cancellationToken)
    {
        var days = query.Period switch { null or "" or "month" => 30, "today" => 1, "week" => 7, "quarter" => 90, _ => 0 };
        if (days == 0)
        {
            return FinanceErrors.InvalidPeriod;
        }

        var now = timeProvider.GetUtcNow();
        var today = DashboardClock.Today(now);
        var start = today.AddDays(1 - days);
        var sales = await analytics.SalesAsync(start, now, cancellationToken);
        var receivables = await queries.ReceivablesAsync(cancellationToken);
        var received = sales.Sum(sale => sale.Total);
        var commissions = sales.SelectMany(sale => sale.Services.Select(line => (sale.MechanicName, sale.OrderId, line)))
            .GroupBy(item => item.MechanicName, StringComparer.Ordinal)
            .Select(group => new CommissionDto(group.Key, group.Select(item => item.OrderId).Distinct().Count(), Money.ToCents(group.Sum(item => item.line.Total)), Money.ToCents(group.Sum(item => item.line.Commission))))
            .OrderByDescending(item => item.Commission).ThenBy(item => item.MechanicName, StringComparer.Ordinal).ToList();
        var kpis = new FinanceKpisDto(
            Money.ToCents(received),
            Money.ToCents(receivables.Sum(item => item.Amount)),
            receivables.Count,
            commissions.Sum(item => item.Commission),
            sales.Count == 0 ? 0 : Money.ToCents(received / sales.Count),
            sales.Count);
        var byMethod = sales.GroupBy(sale => sale.PaymentMethod, StringComparer.Ordinal)
            .Select(group => new PaymentMethodTotalDto(group.Key, Money.ToCents(group.Sum(sale => sale.Total)), group.Count()))
            .OrderByDescending(item => item.Amount).ToList();
        var receipts = sales.OrderByDescending(sale => sale.DeliveredAt).Take(ReceiptLimit)
            .Select(sale => new ReceiptDto(sale.OrderId, sale.Number, sale.CustomerName, sale.DeliveredAt, sale.PaymentMethod, Money.ToCents(sale.Total))).ToList();
        var waiting = receivables.OrderBy(item => item.CompletedAt)
            .Select(item => new ReceivableDto(item.ServiceOrderId, item.Number, item.CustomerName, item.CompletedAt, Money.ToCents(item.Amount), item.CompletedAt is { } done ? Math.Max(0, (int)(now - done).TotalDays) : 0)).ToList();
        return new FinanceSummaryDto(query.Period is null or "" ? "month" : query.Period, Iso(start), Iso(today), kpis, byMethod, receipts, waiting, commissions);
    }

    private static string Iso(DateTimeOffset value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
