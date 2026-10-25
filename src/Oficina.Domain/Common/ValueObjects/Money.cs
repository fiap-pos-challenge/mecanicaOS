using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Common.ValueObjects;

public sealed class Money : ValueObject
{
    private const decimal CentsPerUnit = 100m;

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

    public static decimal Round(decimal amount) => RoundHalfUp(amount);

    public static long ToCents(decimal amount) => (long)Measure.Round(amount * CentsPerUnit, 0);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
    }

    private static decimal RoundHalfUp(decimal value) => Measure.Round(value, 2) + 0.00m;
}
