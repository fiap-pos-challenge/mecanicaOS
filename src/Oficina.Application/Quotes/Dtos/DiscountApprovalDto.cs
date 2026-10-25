namespace Oficina.Application.Quotes.Dtos;

public sealed record DiscountApprovalDto(Guid Id, string Status, DiscountDto Discount, string Reason, string RequestedBy, DateTimeOffset RequestedAt, string? DecidedBy);
