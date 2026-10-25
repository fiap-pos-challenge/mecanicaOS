namespace Oficina.Application.Quotes;

public static class PaymentMethod
{
    public static readonly IReadOnlyList<string> All = ["Pix", "Credit", "Debit", "Cash", "Boleto"];
}
