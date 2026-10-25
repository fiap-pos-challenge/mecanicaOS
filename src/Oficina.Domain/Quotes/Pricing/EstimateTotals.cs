namespace Oficina.Domain.Quotes.Pricing;

public sealed record EstimateTotals(decimal Services, decimal Parts, decimal Subtotal, decimal Discount, decimal Total, bool ExceedsLimit);
