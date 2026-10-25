using System.Globalization;
using Oficina.Application.Quotes.Queries;

namespace Oficina.Application.Quotes;

public static class QuoteTexts
{
    public const string System = "Sistema";
    public const string DiagnosisSuggestion = "Indicado no diagnóstico do mecânico.";
    public const string EmailFailed = "Não foi possível enviar o e-mail. Tente outro canal.";
    public const string ApproveLink = "Ver e aprovar o orçamento";
    public const string DiscountApproved = "Desconto aprovado";
    public const string DiscountRejected = "Desconto recusado";
    private static readonly CultureInfo Brazil = CultureInfo.GetCultureInfo("pt-BR");

    public static string Message(EstimateContext context, decimal total, int days) =>
        $"Olá, {context.CustomerName.Split(' ')[0]}! O orçamento do seu {context.Vehicle} ({context.LicensePlate}) está pronto: {total.ToString("C", Brazil)}. " +
        $"Veja os itens, as fotos e aprove pelo link em até {days} dias. Qualquer dúvida, é só responder.";

    public static string EmailSubject(long number, string vehicle) => $"Orçamento nº {number} · {vehicle}";

    public static string DiscountTitle(long number) => $"Desconto do orçamento nº {number} aguarda aprovação";

    public static string QuoteReference(long number) => $"Orçamento nº {number}";
}
