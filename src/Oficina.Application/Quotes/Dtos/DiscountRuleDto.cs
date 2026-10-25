namespace Oficina.Application.Quotes.Dtos;

public sealed record DiscountRuleDto(Guid Id, string Name, string Type, decimal Value, string Condition, bool IsActive);
