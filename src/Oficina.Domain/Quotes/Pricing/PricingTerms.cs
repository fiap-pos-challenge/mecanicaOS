namespace Oficina.Domain.Quotes.Pricing;

public sealed record PricingTerms(IReadOnlyCollection<DiscountRule> Rules, CustomerFacts Facts, decimal LimitPercent)
{
    public const decimal NoLimit = 100;
}
