using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Enums;
using Oficina.Domain.Customers.Errors;

namespace Oficina.Domain.Customers.ValueObjects;

public sealed class Document : ValueObject
{
    private const int CpfLength = 11;
    private const int CnpjLength = 14;
    private static readonly int[] CnpjFirstWeights = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
    private static readonly int[] CnpjSecondWeights = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

    private Document(string value, DocumentType type)
    {
        Value = value;
        Type = type;
    }

    public string Value { get; }

    public DocumentType Type { get; }

    public static Result<Document> Create(string value)
    {
        var digits = new string(value.Where(char.IsAsciiDigit).ToArray());
        var hasOnlyDigitsAndMask = value.All(character => char.IsAsciiDigit(character) || character is '.' or '-' or '/' or ' ');

        if (!hasOnlyDigitsAndMask || digits.Distinct().Count() == 1)
        {
            return CustomerErrors.InvalidDocument;
        }

        return digits.Length switch
        {
            CpfLength when IsValidCpf(digits) => new Document(digits, DocumentType.Cpf),
            CnpjLength when IsValidCnpj(digits) => new Document(digits, DocumentType.Cnpj),
            _ => CustomerErrors.InvalidDocument,
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static bool IsValidCpf(string digits)
    {
        var firstCheck = CpfCheckDigit(digits, 9);
        var secondCheck = CpfCheckDigit(digits, 10);
        return digits[9] - '0' == firstCheck && digits[10] - '0' == secondCheck;
    }

    private static int CpfCheckDigit(string digits, int length)
    {
        var sum = 0;
        for (var index = 0; index < length; index++)
        {
            sum += (digits[index] - '0') * (length + 1 - index);
        }

        return sum * 10 % 11 % 10;
    }

    private static bool IsValidCnpj(string digits)
    {
        var firstCheck = CnpjCheckDigit(digits, CnpjFirstWeights);
        var secondCheck = CnpjCheckDigit(digits, CnpjSecondWeights);
        return digits[12] - '0' == firstCheck && digits[13] - '0' == secondCheck;
    }

    private static int CnpjCheckDigit(string digits, int[] weights)
    {
        var sum = 0;
        for (var index = 0; index < weights.Length; index++)
        {
            sum += (digits[index] - '0') * weights[index];
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
