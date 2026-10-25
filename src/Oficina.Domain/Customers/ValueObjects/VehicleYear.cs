using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Errors;

namespace Oficina.Domain.Customers.ValueObjects;

public sealed class VehicleYear : ValueObject
{
    public const int FirstAllowedYear = 1900;

    private VehicleYear(int value) => Value = value;

    public int Value { get; }

    public static Result<VehicleYear> Create(int value)
    {
        if (value < FirstAllowedYear)
        {
            return VehicleErrors.InvalidYear;
        }

        return new VehicleYear(value);
    }

    public bool IsAfterNextYear(DateTimeOffset today) => Value > today.Year + 1;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
