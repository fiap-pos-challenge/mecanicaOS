using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Errors;

namespace Oficina.Domain.Customers.ValueObjects;

public sealed class Phone : ValueObject
{
    private Phone(string value) => Value = value;

    public string Value { get; }

    public static Result<Phone> Create(string value)
    {
        var digits = new string(value.Where(char.IsAsciiDigit).ToArray());
        var hasOnlyDigitsAndMask = value.All(character => char.IsAsciiDigit(character) || character is '(' or ')' or '-' or ' ');

        if (!hasOnlyDigitsAndMask || digits.Length is < 10 or > 11 || digits[0] == '0')
        {
            return CustomerErrors.InvalidPhone;
        }

        return new Phone(digits);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
