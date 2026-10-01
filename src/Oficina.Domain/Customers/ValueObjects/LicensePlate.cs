using System.Text.RegularExpressions;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Errors;

namespace Oficina.Domain.Customers.ValueObjects;

public sealed partial class LicensePlate : ValueObject
{
    private LicensePlate(string value) => Value = value;

    public string Value { get; }

    public static Result<LicensePlate> Create(string value)
    {
        var normalized = value.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        if (!OldFormat().IsMatch(normalized) && !MercosulFormat().IsMatch(normalized))
        {
            return VehicleErrors.InvalidLicensePlate;
        }

        return new LicensePlate(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex("^[A-Z]{3}[0-9]{4}$", RegexOptions.CultureInvariant)]
    private static partial Regex OldFormat();

    [GeneratedRegex("^[A-Z]{3}[0-9][A-Z][0-9]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex MercosulFormat();
}
