using System.Text.RegularExpressions;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Common.ValueObjects;

public sealed partial class Email : ValueObject
{
    public const int MaxLength = 160;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength || !EmailPattern().IsMatch(normalized))
        {
            return CommonErrors.InvalidEmail;
        }

        return new Email(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();
}
