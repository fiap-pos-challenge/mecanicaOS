namespace Oficina.Application.Documents.Dtos;

public sealed record DocumentDto(
    string Kind,
    string Code,
    long Number,
    int? Version,
    DateTimeOffset IssuedAt,
    string ShopName,
    string CustomerName,
    string Vehicle,
    IReadOnlyList<DocumentLineDto> Lines,
    decimal ServicesTotal,
    decimal PartsTotal,
    decimal DiscountTotal,
    decimal Total,
    DateTimeOffset? PaidAt,
    IReadOnlyList<string> PaymentMethods);
