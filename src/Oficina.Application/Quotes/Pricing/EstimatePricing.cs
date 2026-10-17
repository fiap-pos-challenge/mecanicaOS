using Oficina.Application.Settings.Models;
using Oficina.Domain.Quotes.Entities;

namespace Oficina.Application.Quotes.Pricing;

public sealed record CustomerFacts(bool Loyal, bool FirstService, bool Fleet);

public sealed record EstimateTotals(decimal Services, decimal Parts, decimal Subtotal, decimal Discount, decimal Total, DiscountRuleSettings? AppliedRule, bool ExceedsLimit);

public static class EstimatePricing
{
    public const int MaxQuantity = 999;

    public static bool IsValidQuantity(decimal quantity, bool fractional) =>
        quantity > 0 && quantity <= MaxQuantity && (fractional ? quantity * 1000 == decimal.Truncate(quantity * 1000) : quantity == decimal.Truncate(quantity));

    public static decimal LineTotal(decimal unitPrice, decimal quantity) => HalfUp(unitPrice * quantity);

    public static bool IsValidDiscount(string type, decimal value) =>
        type == "Percent" ? value > 0 && value <= 100 && value * 100 == decimal.Truncate(value * 100) : type == "Amount" && value > 0;

    public static decimal DiscountAmount(string type, decimal value, decimal subtotal)
    {
        if (subtotal <= 0 || !IsValidDiscount(type, value))
        {
            return 0;
        }

        var raw = type == "Percent" ? HalfUp(subtotal * value / 100) : value;
        return Math.Min(raw, subtotal);
    }

    public static bool RuleMatches(DiscountRuleSettings rule, CustomerFacts facts) =>
        rule.IsActive && rule.Condition switch
        {
            "LoyalCustomer" => facts.Loyal,
            "FirstService" => facts.FirstService,
            "Fleet" => facts.Fleet,
            _ => false,
        };

    public static EstimateTotals Price(IReadOnlyCollection<EstimateLine> lines, string mode, ManualDiscount? manual, IReadOnlyCollection<DiscountRuleSettings> rules, CustomerFacts facts, decimal limitPercent)
    {
        var services = lines.Where(line => line.Kind is "Service" or "Package").Sum(line => LineTotal(line.UnitPrice, line.Quantity));
        var parts = lines.Where(line => line.Kind == "Part").Sum(line => LineTotal(line.UnitPrice, line.Quantity));
        var subtotal = services + parts;
        var automatic = mode == "Automatic"
            ? rules.Where(rule => RuleMatches(rule, facts))
                .Select(rule => (Rule: rule, Amount: DiscountAmount(rule.Type, rule.Value, subtotal)))
                .OrderByDescending(candidate => candidate.Amount)
                .FirstOrDefault()
            : default;
        var discount = mode == "Manual" && manual is not null ? DiscountAmount(manual.Type, manual.Value, subtotal) : automatic.Rule is null ? 0 : automatic.Amount;
        return new EstimateTotals(services, parts, subtotal, discount, subtotal - discount, automatic.Rule, mode == "Manual" && subtotal > 0 && discount * 100 > limitPercent * subtotal);
    }

    private static decimal HalfUp(decimal value)
    {
        var scaled = value * 100;
        var truncated = decimal.Truncate(scaled);
        if (scaled - truncated >= 0.5m)
        {
            truncated += 1;
        }

        return truncated / 100;
    }
}
