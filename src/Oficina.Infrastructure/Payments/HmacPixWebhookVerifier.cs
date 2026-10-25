using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Oficina.Application.Abstractions.Payments;

namespace Oficina.Infrastructure.Payments;

public sealed class HmacPixWebhookVerifier(IOptions<PaymentsOptions> options) : IPixWebhookVerifier
{
    public bool IsValid(string txId, string signature)
    {
        var secret = options.Value.PixWebhookSecret;
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(txId))
        {
            return false;
        }

        var expected = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(txId)));
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(signature.Trim().ToLowerInvariant()));
    }
}
