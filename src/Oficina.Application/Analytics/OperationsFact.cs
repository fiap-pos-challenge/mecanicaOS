namespace Oficina.Application.Analytics;

public sealed record OperationsFact(
    IReadOnlyList<OpenOrderFact> OpenOrders,
    IReadOnlyList<PendingQuoteFact> PendingQuotes,
    IReadOnlyList<LowStockFact> LowStock,
    IReadOnlyList<AnsweredQuoteFact> AnsweredQuotes,
    int MaintenanceDue);
