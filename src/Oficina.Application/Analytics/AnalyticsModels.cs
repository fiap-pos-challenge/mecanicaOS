namespace Oficina.Application.Analytics;

public sealed record SaleServiceLine(string Name, decimal Total, decimal Commission);

public sealed record SalePartLine(string Name, string Category, decimal Quantity, decimal Total, decimal Cost);

public sealed record SaleFact(
    Guid OrderId,
    long Number,
    DateTimeOffset DeliveredAt,
    string CustomerName,
    string MechanicName,
    string PaymentMethod,
    decimal Total,
    IReadOnlyList<SaleServiceLine> Services,
    IReadOnlyList<SalePartLine> Parts);

public sealed record OpenOrderFact(Guid Id, long Number, string Status, string Vehicle, DateTimeOffset? PromisedAt, bool MissingPart, decimal Total);

public sealed record PendingQuoteFact(Guid EstimateId, long Number, DateTimeOffset SentAt, string Channel);

public sealed record LowStockFact(Guid PartId, string Name, decimal Stock, decimal Minimum, string UnitOfMeasure, int DependentOrders);

public sealed record AnsweredQuoteFact(string Status, DateTimeOffset AnsweredAt);

public sealed record OperationsFact(
    IReadOnlyList<OpenOrderFact> OpenOrders,
    IReadOnlyList<PendingQuoteFact> PendingQuotes,
    IReadOnlyList<LowStockFact> LowStock,
    IReadOnlyList<AnsweredQuoteFact> AnsweredQuotes,
    int MaintenanceDue);

public interface IAnalyticsQueries
{
    Task<IReadOnlyList<SaleFact>> SalesAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    Task<OperationsFact> OperationsAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
