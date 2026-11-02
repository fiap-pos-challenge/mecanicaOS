using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Common.ValueObjects;

public sealed class Money : ValueObject
{
    private const decimal CentsPerUnit = 100m;
    private const decimal HalfCent = 0.5m;

    private Money(decimal amount) => Amount = amount;

    public static readonly Money Zero = new(0.00m);

    public decimal Amount { get; }

    public static Result<Money> Create(decimal amount)
    {
        if (amount < 0)
        {
            return CommonErrors.NegativeAmount;
        }

        return new Money(RoundHalfUp(amount));
    }

    public Money Add(Money other) => new(RoundHalfUp(Amount + other.Amount));

    public Money Multiply(Quantity quantity) => new(RoundHalfUp(Amount * quantity.Value));

    public Money Multiply(int factor) => new(RoundHalfUp(Amount * factor));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
    }

    private static decimal RoundHalfUp(decimal value)
    {
        var scaled = value * CentsPerUnit;
        var truncated = decimal.Truncate(scaled);

        if (scaled - truncated >= HalfCent)
        {
            truncated += 1;
        }

        return (truncated / CentsPerUnit) + 0.00m;
    }
}
