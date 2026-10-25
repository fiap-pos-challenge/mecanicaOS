namespace Oficina.Domain.Quotes.Pricing;

public sealed record CustomerFacts(bool Loyal, bool FirstService, bool Fleet)
{
    public static readonly CustomerFacts None = new(false, false, false);
}
