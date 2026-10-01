using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Common.ValueObjects;

public sealed class Quantity : ValueObject
{
    private const decimal Precision = 1000m;

    private Quantity(decimal value) => Value = value;

    public decimal Value { get; }

    public static Result<Quantity> Create(decimal value)
    {
        if (value <= 0 || !HasAtMostThreeDecimals(value))
        {
            return CommonErrors.InvalidQuantity;
        }

        return new Quantity(value);
    }

    public Quantity Add(Quantity other) => new(Value + other.Value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
    private static bool HasAtMostThreeDecimals(decimal value)
    {
        var scaled = value * Precision;
        return scaled == decimal.Truncate(scaled);
    }
}
