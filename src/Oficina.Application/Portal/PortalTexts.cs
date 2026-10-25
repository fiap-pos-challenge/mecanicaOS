namespace Oficina.Application.Portal;

public static class PortalTexts
{
    public const string PaymentBody = "Pix recebido pelo portal do cliente.";

    public static string PaymentTitle(long number) => $"Pagamento da OS {number} confirmado";
}
