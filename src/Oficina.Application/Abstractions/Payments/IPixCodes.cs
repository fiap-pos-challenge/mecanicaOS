namespace Oficina.Application.Abstractions.Payments;

public interface IPixCodes
{
    string Payload(string key, string merchantName, string city, decimal amount, string txId);

    string QrCodeDataUrl(string payload);
}
