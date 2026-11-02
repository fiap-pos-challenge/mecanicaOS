using System.Text.RegularExpressions;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Errors;

namespace Oficina.Domain.Inventory.ValueObjects;

public sealed partial class PartCode : ValueObject
{
    public const int MaxLength = 30;

    private PartCode(string value) => Value = value;

    public string Value { get; }

    public static Result<PartCode> Create(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length is 0 or > MaxLength || !AllowedCharacters().IsMatch(normalized))
        {
            return InventoryErrors.InvalidPartCode;
        }

        return new PartCode(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex(@"^[A-Z0-9.\-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex AllowedCharacters();
}
