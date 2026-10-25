namespace Oficina.Application.Quotes.Dtos;

public sealed record QuoteVersionDto(Guid Id, int Version, string Status, IReadOnlyList<QuoteLineDto> Items, decimal ServicesTotal, decimal PartsTotal, decimal DiscountTotal, decimal Total, string? RejectionReason, DateTimeOffset CreatedAt);
