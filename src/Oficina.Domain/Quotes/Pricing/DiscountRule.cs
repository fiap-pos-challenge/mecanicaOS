namespace Oficina.Domain.Quotes.Pricing;

public sealed record DiscountRule(string Type, decimal Value, string Condition, bool IsActive);
