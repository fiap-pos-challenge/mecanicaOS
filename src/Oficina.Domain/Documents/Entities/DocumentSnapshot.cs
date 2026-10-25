namespace Oficina.Domain.Documents.Entities;

public sealed record DocumentSnapshot(
    long Number,
    int? Version,
    string ShopName,
    string CustomerName,
    string Vehicle,
    IReadOnlyList<DocumentLine> Lines,
    decimal ServicesTotal,
    decimal PartsTotal,
    decimal DiscountTotal,
    decimal Total,
    DateTimeOffset? PaidAt,
    IReadOnlyList<string> PaymentMethods);
