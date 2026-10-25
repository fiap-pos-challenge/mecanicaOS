using System.Globalization;
using MediatR;
using Oficina.Application.Analytics;
using Oficina.Application.Dashboard.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Dashboard;

namespace Oficina.Application.Dashboard.Queries.GetDashboardSnapshot;

public sealed class GetDashboardSnapshotQueryHandler(IAnalyticsQueries analytics, IDashboardActionRepository actions, TimeProvider timeProvider)
    : IRequestHandler<GetDashboardSnapshotQuery, Result<DashboardSnapshotDto>>
{
    private static readonly string[] YardStatuses = ["Received", "InDiagnosis", "WaitingApproval", "InProgress", "Completed"];

    public async Task<Result<DashboardSnapshotDto>> Handle(GetDashboardSnapshotQuery query, CancellationToken cancellationToken)
    {
        if (DashboardClock.PeriodDays(query.Period) is not { } days)
        {
            return DashboardErrors.InvalidPeriod;
        }

        var now = timeProvider.GetUtcNow();
        var start = DashboardClock.Today(now).AddDays(1 - days);
        var previousStart = start.AddDays(-days);
        var sales = await analytics.SalesAsync(previousStart, now, cancellationToken);
        var current = sales.Where(sale => sale.DeliveredAt >= start).ToList();
        var previous = sales.Where(sale => sale.DeliveredAt < start).ToList();
        var operations = await analytics.OperationsAsync(now, cancellationToken);
        var answered = operations.AnsweredQuotes.Where(quote => quote.AnsweredAt >= start).ToList();
        var answeredBefore = operations.AnsweredQuotes.Where(quote => quote.AnsweredAt >= previousStart && quote.AnsweredAt < start).ToList();
        var completed = operations.OpenOrders.Where(order => order.Status == "Completed").ToList();
        var kpis = new List<DashboardKpiDto>
        {
            new("revenue", Money.ToCents(current.Sum(sale => sale.Total)), Money.ToCents(previous.Sum(sale => sale.Total)), null),
            new("openOrders", operations.OpenOrders.Count, null, operations.OpenOrders.Count(order => order.MissingPart)),
            new("approvalRate", Rate(answered), answeredBefore.Count == 0 ? null : Rate(answeredBefore), null),
            new("receivable", Money.ToCents(completed.Sum(order => order.Total)), null, completed.Count),
        };
        var daily = Enumerable.Range(0, days).Select(offset =>
        {
            var day = start.AddDays(offset);
            var sold = current.Where(sale => sale.DeliveredAt >= day && sale.DeliveredAt < day.AddDays(1)).ToList();
            return new DailyRevenueDto(day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Money.ToCents(sold.Sum(sale => sale.Services.Sum(line => line.Total))), Money.ToCents(sold.Sum(sale => sale.Parts.Sum(line => line.Total))));
        }).ToList();
        var top = current.SelectMany(sale => sale.Services).GroupBy(line => line.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => new TopServiceDto(group.First().Name, group.Count())).OrderByDescending(item => item.Quantity).ThenBy(item => item.Name, StringComparer.Ordinal).Take(5).ToList();
        var yard = YardStatuses.Select(status => new YardCountDto(status, operations.OpenOrders.Count(order => order.Status == status))).ToList();
        var handled = await actions.HandledSinceAsync(now - DashboardAttention.HideHandledFor, cancellationToken);
        return new DashboardSnapshotDto(kpis, daily, top, yard, DashboardAttention.Items(operations, now).Where(item => !handled.Contains(item.Id)).ToList());
    }

    private static long Rate(IReadOnlyCollection<AnsweredQuoteFact> answered) =>
        (long)Measure.Percent(answered.Count(quote => quote.Status == "Approved"), answered.Count, 0);
}
