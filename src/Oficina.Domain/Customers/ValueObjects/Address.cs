using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Errors;

namespace Oficina.Domain.Customers.ValueObjects;

public sealed class Address : ValueObject
{
    public const int ZipCodeLength = 8;
    public const int StreetMaxLength = 150;
    public const int NumberMaxLength = 10;
    public const int ComplementMaxLength = 60;
    public const int DistrictMaxLength = 80;
    public const int CityMaxLength = 80;
    public const int StateLength = 2;

    private static readonly HashSet<string> BrazilianStates = new(StringComparer.Ordinal)
    {
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA",
        "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO",
    };

    private Address(string zipCode, string street, string number, string? complement, string district, string city, string state)
    {
        ZipCode = zipCode;
        Street = street;
        Number = number;
        Complement = complement;
        District = district;
        City = city;
        State = state;
    }

    public string ZipCode { get; }

    public string Street { get; }

    public string Number { get; }

    public string? Complement { get; }

    public string District { get; }

    public string City { get; }

    public string State { get; }

    public static Result<Address?> CreateOptional(
        string? zipCode,
        string? street,
        string? number,
        string? complement,
        string? district,
        string? city,
        string? state)
    {
        string?[] required = { zipCode, street, number, district, city, state };
        var complementValue = string.IsNullOrWhiteSpace(complement) ? null : complement.Trim();

        if (required.All(string.IsNullOrWhiteSpace) && complementValue is null)
        {
            return Result<Address?>.Success(null);
        }

        if (string.IsNullOrWhiteSpace(zipCode)
            || string.IsNullOrWhiteSpace(street)
            || string.IsNullOrWhiteSpace(number)
            || string.IsNullOrWhiteSpace(district)
            || string.IsNullOrWhiteSpace(city)
            || string.IsNullOrWhiteSpace(state))
        {
            return CustomerErrors.IncompleteAddress;
        }

        var zipDigits = zipCode.Replace("-", string.Empty, StringComparison.Ordinal).Trim();
        if (zipDigits.Length != ZipCodeLength || !zipDigits.All(char.IsAsciiDigit))
        {
            return CustomerErrors.InvalidZipCode;
        }

        var stateCode = state.Trim().ToUpperInvariant();
        if (!BrazilianStates.Contains(stateCode))
        {
            return CustomerErrors.InvalidState;
        }

        var streetValue = street.Trim();
        var numberValue = number.Trim();
        var districtValue = district.Trim();
        var cityValue = city.Trim();

        if (streetValue.Length > StreetMaxLength
            || numberValue.Length > NumberMaxLength
            || districtValue.Length > DistrictMaxLength
            || cityValue.Length > CityMaxLength
            || complementValue?.Length > ComplementMaxLength)
        {
            return CustomerErrors.InvalidAddress;
        }

        return new Address(zipDigits, streetValue, numberValue, complementValue, districtValue, cityValue, stateCode);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ZipCode;
        yield return Street;
        yield return Number;
        yield return Complement;
        yield return District;
        yield return City;
        yield return State;
    }
}
