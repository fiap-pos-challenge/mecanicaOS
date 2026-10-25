using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Oficina.Application.Abstractions.Security;

namespace Oficina.Infrastructure.Security;

public sealed class DataProtectionSecretProtector(IDataProtectionProvider provider) : ISecretProtector
{
    public const string Purpose = "Oficina.Identity.TwoFactorSecret";
    public const string Prefix = "dp:";

    private readonly IDataProtector _protector = provider.CreateProtector(Purpose);

    public string Protect(string secret) => Prefix + _protector.Protect(secret);

    public string? Unprotect(string protectedSecret)
    {
        if (!IsProtected(protectedSecret))
        {
            return protectedSecret;
        }

        try
        {
            return _protector.Unprotect(protectedSecret[Prefix.Length..]);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public bool IsProtected(string value) => value.StartsWith(Prefix, StringComparison.Ordinal);
}
