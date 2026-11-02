using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.Domain.ServiceOrders.ValueObjects;

public sealed class ServiceOrderNumber : ValueObject
{
    private ServiceOrderNumber(long value) => Value = value;

    public long Value { get; }

    public static Result<ServiceOrderNumber> Create(long value)
    {
        if (value <= 0)
        {
            return ServiceOrderErrors.InvalidNumber;
        }

        return new ServiceOrderNumber(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
