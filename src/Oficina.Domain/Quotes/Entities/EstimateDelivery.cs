namespace Oficina.Domain.Quotes.Entities;

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
