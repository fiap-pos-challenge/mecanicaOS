using System.Security.Cryptography;
using System.Text;

namespace Oficina.Application.Identity.Invitations;

public static class InvitationToken
{
    public const int MaxLength = 128;

    private const int TokenBytes = 32;

    public static string Generate() => Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));

    public static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
