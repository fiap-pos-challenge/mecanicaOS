using System.Globalization;
using Microsoft.Extensions.Logging;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Analytics;
using Oficina.Application.Common.Numbers;
using Oficina.Application.Quotes.Queries;
using Oficina.Application.Settings;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Dashboard;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Domain.Quotes.Repositories;

namespace Oficina.Application.Dashboard;

public sealed partial class DashboardService(
    IAnalyticsQueries analytics,
    IDashboardActionRepository actions,
    SettingsStore settings,
    IPartRepository parts,
    ISupplierQueries suppliers,
    IPurchaseOrderRepository purchaseOrders,
    IEstimateRepository estimates,
    IQuoteQueries quoteQueries,
    IEmailSender emailSender,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<DashboardService> logger)
{
    public static readonly string[] Cards = ["kpis", "dailyRevenue", "attention", "topServices", "yard"];
    public static readonly TimeSpan Brazil = TimeSpan.FromHours(-3);
    private static readonly string[] YardStatuses = ["Received", "InDiagnosis", "WaitingApproval", "InProgress", "Completed"];
    private static readonly TimeSpan QuoteAnswerWindow = TimeSpan.FromDays(2);
    private static readonly TimeSpan HideHandledFor = TimeSpan.FromDays(1);

    public async Task<Result<DashboardSnapshotDto>> SnapshotAsync(string? period, CancellationToken cancellationToken)
    {
        var days = period switch { null or "" or "today" => 1, "week" => 7, "month" => 30, _ => 0 };
        if (days == 0)
        {
            return DashboardErrors.InvalidPeriod;
        }

        var now = timeProvider.GetUtcNow();
        var today = new DateTimeOffset(now.ToOffset(Brazil).Date, Brazil);
        var start = today.AddDays(1 - days);
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
            new("revenue", Cents(current.Sum(sale => sale.Total)), Cents(previous.Sum(sale => sale.Total)), null),
            new("openOrders", operations.OpenOrders.Count, null, operations.OpenOrders.Count(order => order.MissingPart)),
            new("approvalRate", Rate(answered), answeredBefore.Count == 0 ? null : Rate(answeredBefore), null),
            new("receivable", Cents(completed.Sum(order => order.Total)), null, completed.Count),
        };
        var daily = Enumerable.Range(0, days).Select(offset =>
        {
            var day = start.AddDays(offset);
            var sold = current.Where(sale => sale.DeliveredAt >= day && sale.DeliveredAt < day.AddDays(1)).ToList();
            return new DailyRevenueDto(day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Cents(sold.Sum(sale => sale.Services.Sum(line => line.Total))), Cents(sold.Sum(sale => sale.Parts.Sum(line => line.Total))));
        }).ToList();
        var top = current.SelectMany(sale => sale.Services).GroupBy(line => line.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => new TopServiceDto(group.First().Name, group.Count())).OrderByDescending(item => item.Quantity).ThenBy(item => item.Name, StringComparer.Ordinal).Take(5).ToList();
        var yard = YardStatuses.Select(status => new YardCountDto(status, operations.OpenOrders.Count(order => order.Status == status))).ToList();
        var handled = await actions.HandledSinceAsync(now - HideHandledFor, cancellationToken);
        return new DashboardSnapshotDto(kpis, daily, top, yard, Attention(operations, now).Where(item => !handled.Contains(item.Id)).ToList());
    }

    public async Task<Result<DashboardLayoutDto>> LayoutAsync(CancellationToken cancellationToken) =>
        new DashboardLayoutDto(await settings.GetAsync<List<DashboardCardDto>>(LayoutKey(), [], cancellationToken));

    public async Task<Result<DashboardLayoutDto>> SaveLayoutAsync(IReadOnlyList<DashboardCardDto>? cards, CancellationToken cancellationToken)
    {
        if (cards is null || cards.Count > Cards.Length || cards.Any(card => !Cards.Contains(card.Id)) || cards.Select(card => card.Id).Distinct(StringComparer.Ordinal).Count() != cards.Count)
        {
            return DashboardErrors.InvalidLayout;
        }

        var saved = cards.Select(card => new DashboardCardDto(card.Id, card.Visible)).ToList();
        await settings.SaveAsync(LayoutKey(), saved, cancellationToken);
        return (await unitOfWork.CommitAsync(cancellationToken)).Error is { } error ? error : new DashboardLayoutDto(saved);
    }

    public async Task<Result> ActAsync(string id, string? action, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var operations = await analytics.OperationsAsync(now, cancellationToken);
        var handled = await actions.HandledSinceAsync(now - HideHandledFor, cancellationToken);
        if (Attention(operations, now).FirstOrDefault(item => item.Id == id && !handled.Contains(item.Id)) is not { } item)
        {
            return DashboardErrors.AttentionNotFound;
        }

        if (item.Action is null || item.Action != action)
        {
            return DashboardErrors.ActionNotAllowed;
        }

        var done = action switch
        {
            "CreatePurchaseOrder" => await PurchaseAsync(operations.LowStock.First(part => $"stock-{part.PartId:N}" == id), cancellationToken),
            "SendReminder" => await RemindAsync(operations.PendingQuotes.First(quote => $"quote-{quote.EstimateId:N}" == id), cancellationToken),
            _ => await DeadlineAsync(operations.OpenOrders.First(order => $"late-{order.Id:N}" == id), cancellationToken),
        };
        if (done.Error is { } error)
        {
            return error;
        }

        actions.Add(DashboardAction.Record(id, action, currentUser.UserId, now));
        return await unitOfWork.CommitAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Dashboard action {Action} queued for WhatsApp")]
    private static partial void LogQueued(ILogger logger, string action);

    private static long Cents(decimal value) => (long)Rounding.HalfUp(value * 100, 0);

    private static long Rate(IReadOnlyCollection<AnsweredQuoteFact> answered) =>
        answered.Count == 0 ? 0 : (long)Rounding.HalfUp(answered.Count(quote => quote.Status == "Approved") * 100m / answered.Count, 0);

    private static IEnumerable<AttentionItemDto> Attention(OperationsFact operations, DateTimeOffset now)
    {
        foreach (var part in operations.LowStock.OrderBy(part => part.Stock - part.Minimum))
        {
            var quantity = part.Stock.ToString("0.###", CultureInfo.GetCultureInfo("pt-BR"));
            var minimum = part.Minimum.ToString("0.###", CultureInfo.GetCultureInfo("pt-BR"));
            yield return new AttentionItemDto($"stock-{part.PartId:N}", "LowStock", part.Stock <= 0 ? "bad" : "warn", part.Name,
                $"{quantity} {part.UnitOfMeasure} · mínimo {minimum}{(part.DependentOrders > 0 ? $" · {part.DependentOrders} OS dependem dela" : string.Empty)}",
                "CreatePurchaseOrder", new Dictionary<string, string> { ["kind"] = "inventory" }, false);
        }

        foreach (var quote in operations.PendingQuotes.Where(quote => now - quote.SentAt >= QuoteAnswerWindow).OrderBy(quote => quote.SentAt))
        {
            yield return new AttentionItemDto($"quote-{quote.EstimateId:N}", "QuoteWithoutAnswer", "warn", $"Orçamento nº {quote.Number} sem resposta",
                $"Enviado há {(int)(now - quote.SentAt).TotalDays} dias", "SendReminder", new Dictionary<string, string> { ["kind"] = "quote", ["id"] = quote.EstimateId.ToString() }, false);
        }

        foreach (var order in operations.OpenOrders.Where(order => order.PromisedAt is { } promised && promised < now && order.Status != "Completed").OrderBy(order => order.PromisedAt))
        {
            yield return new AttentionItemDto($"late-{order.Id:N}", "LateOrder", "warn", $"OS nº {order.Number} passou do prazo",
                $"{order.Vehicle} · prometida para {order.PromisedAt!.Value.ToOffset(Brazil).ToString("dd/MM HH:mm", CultureInfo.InvariantCulture)} · avisar o cliente?", "SendNewDeadline",
                new Dictionary<string, string> { ["kind"] = "serviceOrder", ["id"] = order.Id.ToString() }, false);
        }

        if (operations.MaintenanceDue > 0)
        {
            yield return new AttentionItemDto("maintenance", "MaintenanceDue", "info", $"{operations.MaintenanceDue} clientes com revisão vencendo",
                "Última visita há mais de 6 meses", null, new Dictionary<string, string> { ["kind"] = "customers" }, true);
        }
    }

    private string LayoutKey() => $"dash:{currentUser.UserId.GetValueOrDefault():N}";

    private async Task<Result> PurchaseAsync(LowStockFact fact, CancellationToken cancellationToken)
    {
        if (await parts.GetByIdAsync(fact.PartId, cancellationToken) is not { } part || await suppliers.PreferredAsync(part.Supplier, cancellationToken) is not { } supplierId)
        {
            return DashboardErrors.NoSupplier;
        }

        var quantity = Math.Max(1, Math.Ceiling((part.MinimumStock * 2) - part.StockQuantity));
        var order = PurchaseOrder.Create(await purchaseOrders.NextNumberAsync(cancellationToken), supplierId, [new PurchaseOrderLine(part.Id, quantity, part.Cost.Amount)]);
        if (order.Error is { } error)
        {
            return error;
        }

        purchaseOrders.Add(order.Value);
        return Result.Success();
    }

    private async Task<Result> RemindAsync(PendingQuoteFact fact, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(fact.EstimateId, cancellationToken) is { Delivery: { } delivery } estimate
            && await quoteQueries.ContextAsync(estimate.ServiceOrderId, cancellationToken) is { ConsentEmail: true, Email: { } email } context)
        {
            await emailSender.SendAsync(new EmailMessage(email, $"Lembrete: orçamento nº {estimate.Number}",
                $"<p>Olá, {System.Net.WebUtility.HtmlEncode(context.CustomerName.Split(' ')[0])}! O orçamento do seu {System.Net.WebUtility.HtmlEncode(context.Vehicle)} ainda aguarda sua resposta.</p><p><a href=\"{delivery.Link}\">Ver o orçamento</a></p>"), cancellationToken);
            return Result.Success();
        }

        LogQueued(logger, "SendReminder");
        return Result.Success();
    }

    private async Task<Result> DeadlineAsync(OpenOrderFact fact, CancellationToken cancellationToken)
    {
        if (await quoteQueries.ContextAsync(fact.Id, cancellationToken) is { ConsentEmail: true, Email: { } email } context)
        {
            await emailSender.SendAsync(new EmailMessage(email, $"OS nº {fact.Number}: novo prazo",
                $"<p>Olá, {System.Net.WebUtility.HtmlEncode(context.CustomerName.Split(' ')[0])}! O serviço no seu {System.Net.WebUtility.HtmlEncode(context.Vehicle)} vai precisar de mais tempo. Entraremos em contato com o novo prazo.</p>"), cancellationToken);
            return Result.Success();
        }

        LogQueued(logger, "SendNewDeadline");
        return Result.Success();
    }
}

public interface ISupplierQueries
{
    Task<Guid?> PreferredAsync(string? name, CancellationToken cancellationToken);
}
