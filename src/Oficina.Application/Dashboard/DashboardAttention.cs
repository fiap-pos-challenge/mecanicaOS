using System.Globalization;
using Oficina.Application.Analytics;
using Oficina.Application.Dashboard.Dtos;

namespace Oficina.Application.Dashboard;

public static class DashboardAttention
{
    public const string CreatePurchaseOrder = "CreatePurchaseOrder";
    public const string SendReminder = "SendReminder";
    public const string SendNewDeadline = "SendNewDeadline";
    public static readonly TimeSpan HideHandledFor = TimeSpan.FromDays(1);
    private static readonly TimeSpan QuoteAnswerWindow = TimeSpan.FromDays(2);
    private static readonly CultureInfo Portuguese = CultureInfo.GetCultureInfo("pt-BR");

    public static string StockId(Guid partId) => $"stock-{partId:N}";

    public static string QuoteId(Guid estimateId) => $"quote-{estimateId:N}";

    public static string LateId(Guid orderId) => $"late-{orderId:N}";

    public static IEnumerable<AttentionItemDto> Items(OperationsFact operations, DateTimeOffset now)
    {
        foreach (var part in operations.LowStock.OrderBy(part => part.Stock - part.Minimum))
        {
            var quantity = part.Stock.ToString("0.###", Portuguese);
            var minimum = part.Minimum.ToString("0.###", Portuguese);
            yield return new AttentionItemDto(StockId(part.PartId), "LowStock", part.Stock <= 0 ? "bad" : "warn", part.Name,
                $"{quantity} {part.UnitOfMeasure} · mínimo {minimum}{(part.DependentOrders > 0 ? $" · {part.DependentOrders} OS dependem dela" : string.Empty)}",
                CreatePurchaseOrder, new Dictionary<string, string> { ["kind"] = "inventory" }, false);
        }

        foreach (var quote in operations.PendingQuotes.Where(quote => now - quote.SentAt >= QuoteAnswerWindow).OrderBy(quote => quote.SentAt))
        {
            yield return new AttentionItemDto(QuoteId(quote.EstimateId), "QuoteWithoutAnswer", "warn", $"Orçamento nº {quote.Number} sem resposta",
                $"Enviado há {(int)(now - quote.SentAt).TotalDays} dias", SendReminder, new Dictionary<string, string> { ["kind"] = "quote", ["id"] = quote.EstimateId.ToString() }, false);
        }

        foreach (var order in operations.OpenOrders.Where(order => order.PromisedAt is { } promised && promised < now && order.Status != "Completed").OrderBy(order => order.PromisedAt))
        {
            var promised = order.PromisedAt.GetValueOrDefault().ToOffset(DashboardClock.Brazil).ToString("dd/MM HH:mm", CultureInfo.InvariantCulture);
            yield return new AttentionItemDto(LateId(order.Id), "LateOrder", "warn", $"OS nº {order.Number} passou do prazo",
                $"{order.Vehicle} · prometida para {promised} · avisar o cliente?", SendNewDeadline,
                new Dictionary<string, string> { ["kind"] = "serviceOrder", ["id"] = order.Id.ToString() }, false);
        }

        if (operations.MaintenanceDue > 0)
        {
            yield return new AttentionItemDto("maintenance", "MaintenanceDue", "info", $"{operations.MaintenanceDue} clientes com revisão vencendo",
                "Última visita há mais de 6 meses", null, new Dictionary<string, string> { ["kind"] = "customers" }, true);
        }
    }
}
