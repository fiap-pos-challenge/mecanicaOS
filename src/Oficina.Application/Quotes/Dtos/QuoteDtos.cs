namespace Oficina.Application.Quotes.Dtos;

public sealed record DiscountDto(string Type, decimal Value);

public sealed record ManualDiscountDto(string Type, decimal Value, string Reason);

public sealed record QuoteLineDto(Guid Id, string Kind, Guid ItemId, string Name, decimal UnitPrice, decimal Quantity, string Source, bool Fractional);

public sealed record QuoteCustomerDto(Guid Id, string Name, string? MaskedPhone, string? MaskedEmail, bool ConsentWhatsApp, bool ConsentEmail, bool Loyal, bool FirstService, bool Fleet);

public sealed record QuoteVehicleDto(string LicensePlate, string Description, int Mileage);

public sealed record DiscountApprovalDto(Guid Id, string Status, DiscountDto Discount, string Reason, string RequestedBy, DateTimeOffset RequestedAt, string? DecidedBy);

public sealed record QuoteDeliveryDto(Guid Id, string Channel, string Status, string Link, DateTimeOffset ExpiresAt, DateTimeOffset? SentAt, DateTimeOffset? OpenedAt, string? FailureReason, int Attempts);

public sealed record PaymentPlanDto(IReadOnlyList<string> Methods, int MaxInstallments);

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

public sealed record QuoteSummaryDto(Guid Id, long Number, int Version, string Status, string CustomerName, string LicensePlate, decimal Total, string? DeliveryStatus, bool ApprovalPending, DateTimeOffset UpdatedAt);

public sealed record QuoteVersionDto(Guid Id, int Version, string Status, IReadOnlyList<QuoteLineDto> Items, decimal ServicesTotal, decimal PartsTotal, decimal DiscountTotal, decimal Total, string? RejectionReason, DateTimeOffset CreatedAt);

public sealed record QuoteVersionListDto(IReadOnlyList<QuoteVersionDto> Items);

public sealed record QuoteOptionStockDto(decimal Available, int? LeadTimeDays, string? Supplier);

public sealed record QuoteOptionDto(string Kind, Guid ItemId, string Name, string? Detail, decimal UnitPrice, bool Fractional, QuoteOptionStockDto? Stock, string? Reason);

public sealed record DiscountRuleDto(Guid Id, string Name, string Type, decimal Value, string Condition, bool IsActive);

public sealed record PaymentFeeDto(string Method, decimal FeePercent, int SettlementDays);

public sealed record PaymentSettingsDto(IReadOnlyList<string> Methods, int MaxInstallments, int InterestFreeInstallments, decimal MonthlyInterestPercent, IReadOnlyList<PaymentFeeDto> Fees);

public sealed record QuoteOptionsDto(
    IReadOnlyList<QuoteOptionDto> Packages,
    IReadOnlyList<QuoteOptionDto> Services,
    IReadOnlyList<QuoteOptionDto> Parts,
    IReadOnlyList<QuoteOptionDto> Suggestions,
    IReadOnlyList<DiscountRuleDto> DiscountRules,
    decimal DiscountLimitPercent,
    PaymentSettingsDto Payment,
    IReadOnlyList<int> ExpirationDays);

public sealed record OpenServiceOrderDto(Guid Id, long Number, string CustomerName, string LicensePlate, string Vehicle);

public sealed record OpenServiceOrderListDto(IReadOnlyList<OpenServiceOrderDto> Items);

public sealed record SendQuoteResultDto(QuoteDeliveryDto Delivery, string? WhatsAppUrl);

public sealed record MessageDraftDto(string Message);

public sealed record QuoteDraftItemInput(string Kind, Guid ItemId, decimal Quantity, decimal? UnitPrice, string? Source);

public sealed record QuoteDraftInput(IReadOnlyList<QuoteDraftItemInput> Items, string DiscountMode, ManualDiscountDto? ManualDiscount, PaymentPlanDto? Payment);
