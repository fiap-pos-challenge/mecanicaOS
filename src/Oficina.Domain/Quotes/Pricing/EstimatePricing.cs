using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Quotes.Entities;

namespace Oficina.Domain.Quotes.Pricing;

public static class EstimatePricing
{
    public const int MaxQuantity = 999;
    public const string PercentDiscount = "Percent";
    public const string AmountDiscount = "Amount";

    public static bool IsValidQuantity(decimal quantity, bool fractional) =>
        quantity > 0 && quantity <= MaxQuantity && (fractional ? quantity * 1000 == decimal.Truncate(quantity * 1000) : quantity == decimal.Truncate(quantity));

    public static decimal LineTotal(decimal unitPrice, decimal quantity) => Money.Round(unitPrice * quantity);

    public static bool IsValidDiscount(string type, decimal value) =>
        type == PercentDiscount ? value > 0 && value <= 100 && value * 100 == decimal.Truncate(value * 100) : type == AmountDiscount && value > 0;

    public static decimal DiscountAmount(string type, decimal value, decimal subtotal)
    {
        if (subtotal <= 0 || !IsValidDiscount(type, value))
        {
            return 0;
        }

        var raw = type == PercentDiscount ? Money.Round(subtotal * value / 100) : value;
        return Math.Min(raw, subtotal);
    }

    public static bool RuleMatches(DiscountRule rule, CustomerFacts facts) =>
        rule.IsActive && rule.Condition switch
        {
            "LoyalCustomer" => facts.Loyal,
            "FirstService" => facts.FirstService,
            "Fleet" => facts.Fleet,
            _ => false,
        };

    public static EstimateTotals Price(IReadOnlyCollection<EstimateLine> lines, string mode, ManualDiscount? manual, PricingTerms terms)
    {
        var services = lines.Where(line => line.Kind is EstimateLineKind.Service or EstimateLineKind.Package).Sum(line => LineTotal(line.UnitPrice, line.Quantity));
        var parts = lines.Where(line => line.Kind == EstimateLineKind.Part).Sum(line => LineTotal(line.UnitPrice, line.Quantity));
        var subtotal = services + parts;
        var automatic = mode == DiscountMode.Automatic
            ? terms.Rules.Where(rule => RuleMatches(rule, terms.Facts))
                .Select(rule => DiscountAmount(rule.Type, rule.Value, subtotal))
                .DefaultIfEmpty(0)
                .Max()
            : 0;
        var discount = mode == DiscountMode.Manual && manual is not null ? DiscountAmount(manual.Type, manual.Value, subtotal) : automatic;
        return new EstimateTotals(services, parts, subtotal, discount, subtotal - discount, mode == DiscountMode.Manual && subtotal > 0 && discount * 100 > terms.LimitPercent * subtotal);
    }
}
