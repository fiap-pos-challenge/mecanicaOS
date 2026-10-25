namespace Oficina.Application.Settings.Models;

public sealed record DiscountSettings(IReadOnlyList<DiscountRuleSettings> Rules, IReadOnlyList<DiscountLimitSettings> Limits);
