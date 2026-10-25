using System.Globalization;
using System.Net;
using Oficina.Application.Documents.Dtos;

namespace Oficina.Application.Documents;

public static class DocumentTexts
{
    private static readonly CultureInfo Brazil = CultureInfo.GetCultureInfo("pt-BR");

    public static string Title(string kind) => kind switch { DocumentIssuer.QuoteKind => "Orçamento", DocumentIssuer.ReceiptKind => "Recibo", _ => "Ordem de serviço" };

    public static IReadOnlyList<string> PdfLines(DocumentDto document, string verificationLink)
    {
        var lines = new List<string>
        {
            document.ShopName,
            $"{Title(document.Kind)} nº {document.Number}{(document.Version is { } version ? $" · versão {version}" : string.Empty)}",
            $"Emitido em {document.IssuedAt.ToString("dd/MM/yyyy HH:mm", Brazil)}",
            $"Cliente: {document.CustomerName}",
            $"Veículo: {document.Vehicle}",
            string.Empty,
        };
        lines.AddRange(document.Lines.Select(line => $"{line.Name} · {line.Quantity.ToString("0.###", Brazil)} x {line.UnitPrice.ToString("C", Brazil)} = {line.Total.ToString("C", Brazil)}"));
        lines.Add(string.Empty);
        lines.Add($"Serviços: {document.ServicesTotal.ToString("C", Brazil)}   Peças: {document.PartsTotal.ToString("C", Brazil)}   Desconto: {document.DiscountTotal.ToString("C", Brazil)}");
        lines.Add($"Total: {document.Total.ToString("C", Brazil)}");
        if (document.PaidAt is { } paid)
        {
            lines.Add($"Pago em {paid.ToString("dd/MM/yyyy HH:mm", Brazil)}");
        }

        lines.Add(string.Empty);
        lines.Add($"Código {document.Code} · confira em {verificationLink}");
        return lines;
    }

    public static string EmailSubject(string shopName, string code) => $"{shopName} · documento {code}";

    public static string EmailBody(string customerFirstName, string code, string link) =>
        $"<p>Olá, {WebUtility.HtmlEncode(customerFirstName)}! Segue o documento {code}.</p><p><a href=\"{link}\">Conferir o documento</a></p>";
}
