using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.Domain.ServiceOrders.ValueObjects;

public sealed class ApprovalToken : ValueObject
{
    public const int HashLength = 64;

    private ApprovalToken(string hash) => Hash = hash;

    public string Hash { get; }

    public static Result<ApprovalToken> Create(string hash)
    {
        var normalized = hash.Trim().ToLowerInvariant();

        if (normalized.Length != HashLength || !normalized.All(char.IsAsciiHexDigit))
        {
            return ServiceOrderErrors.InvalidApprovalToken;
        }

        return new ApprovalToken(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hash;
    }
}
