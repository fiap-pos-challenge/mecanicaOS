namespace Oficina.Api.Contracts.Quotes;

public sealed record DiscountApprovalBody(string Type, decimal Value, string Reason);
