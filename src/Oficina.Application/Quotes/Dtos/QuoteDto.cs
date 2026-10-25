namespace Oficina.Application.Quotes.Dtos;

public sealed record QuoteDto(
    Guid Id,
    long Number,
    Guid ServiceOrderId,
    long ServiceOrderNumber,
    int Version,
    string Status,
    QuoteCustomerDto Customer,
    QuoteVehicleDto Vehicle,
    string? Diagnosis,
    IReadOnlyList<QuoteLineDto> Items,
    string DiscountMode,
    ManualDiscountDto? ManualDiscount,
    PaymentPlanDto Payment,
    DiscountApprovalDto? Approval,
    QuoteDeliveryDto? Delivery,
    string? RejectionReason,
    decimal ServicesTotal,
    decimal PartsTotal,
    decimal DiscountTotal,
    decimal Total,
    DateTimeOffset UpdatedAt);
