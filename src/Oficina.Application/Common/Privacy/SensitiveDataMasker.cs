using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Enums;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Application.Common.Privacy;

public static class SensitiveDataMasker
{
    public const char Hidden = '•';

    public static string MaskDocument(Document document)
    {
        var digits = document.Value;
        return document.Type == DocumentType.Cpf
            ? $"{Hide(3)}.{digits[3..6]}.{digits[6..9]}-{Hide(2)}"
            : $"{digits[..2]}.{digits[2..5]}.{Hide(3)}/{Hide(4)}-{digits[12..]}";
    }

    public static string MaskEmail(Email email)
    {
        var at = email.Value.IndexOf('@', StringComparison.Ordinal);
        return $"{email.Value[0]}{Hide(3)}{email.Value[at..]}";
    }

    public static string MaskPhone(Phone phone) => $"{Hide(phone.Value.Length - 4)}{phone.Value[^4..]}";

    private static string Hide(int count) => new(Hidden, count);
}
