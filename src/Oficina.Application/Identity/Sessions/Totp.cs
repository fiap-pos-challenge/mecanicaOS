using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Oficina.Application.Identity.Sessions;

public static class Totp
{
    public const int Digits = 6;
    public const int StepSeconds = 30;

    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const int SecretBytes = 20;

    public static string NewSecret() => ToBase32(RandomNumberGenerator.GetBytes(SecretBytes));

    public static string Code(string secret, DateTimeOffset at)
    {
        var counter = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, at.ToUnixTimeSeconds() / StepSeconds);
        var hash = HMACSHA1.HashData(FromBase32(secret), counter);
        var offset = hash[^1] & 0x0F;
        var value = (BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset, 4)) & 0x7FFFFFFF) % 1_000_000;
        return value.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    public static bool Verify(string secret, string code, DateTimeOffset now)
    {
        var normalized = code.Trim();
        if (normalized.Length != Digits || !normalized.All(char.IsAsciiDigit))
        {
            return false;
        }

        return new[] { -1, 0, 1 }.Any(step => CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Code(secret, now.AddSeconds(step * StepSeconds))),
            Encoding.ASCII.GetBytes(normalized)));
    }

    public static string Uri(string issuer, string account, string secret) =>
        $"otpauth://totp/{System.Uri.EscapeDataString(issuer)}:{System.Uri.EscapeDataString(account)}?secret={secret}&issuer={System.Uri.EscapeDataString(issuer)}&digits={Digits}&period={StepSeconds}";

    private static string ToBase32(byte[] bytes)
    {
        var builder = new StringBuilder();
        for (var bit = 0; bit < bytes.Length * 8; bit += 5)
        {
            var index = 0;
            for (var offset = 0; offset < 5; offset++)
            {
                var position = bit + offset;
                index <<= 1;
                if (position < bytes.Length * 8 && (bytes[position / 8] & (0x80 >> (position % 8))) != 0)
                {
                    index |= 1;
                }
            }

            builder.Append(Alphabet[index]);
        }

        return builder.ToString();
    }

    private static byte[] FromBase32(string secret)
    {
        var bits = secret.TrimEnd('=').ToUpperInvariant().Select(character => Alphabet.IndexOf(character)).Where(index => index >= 0).ToArray();
        var bytes = new byte[bits.Length * 5 / 8];
        for (var bit = 0; bit < bytes.Length * 8; bit++)
        {
            if ((bits[bit / 5] & (0x10 >> (bit % 5))) != 0)
            {
                bytes[bit / 8] |= (byte)(0x80 >> (bit % 8));
            }
        }

        return bytes;
    }
}
