namespace Oficina.Application.Abstractions.Payments;

public interface IPixWebhookVerifier
{
    bool IsValid(string txId, string signature);
}
