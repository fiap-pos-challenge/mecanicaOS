namespace Oficina.Domain.Quotes.Entities;

public sealed record EstimateLine(Guid Id, string Kind, Guid ItemId, string Name, decimal UnitPrice, decimal Quantity, string Source, bool Fractional);

public sealed record ManualDiscount(string Type, decimal Value, string Reason);

public sealed record DiscountApproval(Guid Id, string Status, string Type, decimal Value, string Reason, string RequestedBy, DateTimeOffset RequestedAt, string? DecidedBy);

public sealed record EstimateDelivery(
    Guid Id,
    string Channel,
    string Status,
    string Link,
    string TokenHash,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? SentAt,
    DateTimeOffset? OpenedAt,
    string? FailureReason,
    int Attempts);

public static class EstimateStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

public static class DeliveryStatus
{
    public const string Queued = "Queued";
    public const string Sent = "Sent";
    public const string Opened = "Opened";
    public const string Failed = "Failed";
}
