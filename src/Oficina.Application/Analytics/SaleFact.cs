namespace Oficina.Application.Analytics;

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
