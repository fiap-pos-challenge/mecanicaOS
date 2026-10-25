namespace Oficina.Application.Settings.Models;

public sealed record DiscountRuleSettings(Guid Id, string Name, string Type, decimal Value, string Condition, bool IsActive);
