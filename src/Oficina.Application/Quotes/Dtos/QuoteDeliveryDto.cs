namespace Oficina.Application.Quotes.Dtos;

public sealed record QuoteDeliveryDto(Guid Id, string Channel, string Status, string Link, DateTimeOffset ExpiresAt, DateTimeOffset? SentAt, DateTimeOffset? OpenedAt, string? FailureReason, int Attempts);
