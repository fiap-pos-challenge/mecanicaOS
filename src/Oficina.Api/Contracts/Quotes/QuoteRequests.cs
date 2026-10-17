namespace Oficina.Api.Contracts.Quotes;

public sealed record CreateQuoteBody(Guid ServiceOrderId);

public sealed record DiscountApprovalBody(string Type, decimal Value, string Reason);

public sealed record MessageDraftBody(string? Channel);

public sealed record SendQuoteBody(string Channel, int ExpiresInDays, string Message);

public sealed record RejectQuoteBody(string Reason);
