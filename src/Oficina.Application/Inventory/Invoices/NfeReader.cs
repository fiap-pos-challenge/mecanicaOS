using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Oficina.Application.Inventory.Invoices;

public sealed record NfeLine(string Description, string? Barcode, decimal Quantity, decimal UnitCost);

public sealed record Nfe(string SupplierName, string? SupplierDocument, string Number, DateTimeOffset IssuedAt, IReadOnlyList<NfeLine> Lines);

public static class NfeReader
{
    public const int MaxContentLength = 2_000_000;

    public static Nfe? Read(string fileName, string content)
    {
        if (!fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(content) || content.Length > MaxContentLength)
        {
            return null;
        }

        try
        {
            var base64 = content.Contains(',', StringComparison.Ordinal) && content.StartsWith("data:", StringComparison.Ordinal) ? content[(content.IndexOf(',', StringComparison.Ordinal) + 1)..] : content;
            var xml = Encoding.UTF8.GetString(Convert.FromBase64String(base64.Trim()));
            using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            var document = XDocument.Load(reader);
            var elements = document.Descendants().ToList();
            string? Value(XElement? parent, string name) => parent?.Descendants().FirstOrDefault(element => element.Name.LocalName == name)?.Value;
            var emit = elements.FirstOrDefault(element => element.Name.LocalName == "emit");
            var ide = elements.FirstOrDefault(element => element.Name.LocalName == "ide");
            var lines = elements.Where(element => element.Name.LocalName == "prod")
                .Select(product =>
                {
                    var barcode = Value(product, "cEAN");
                    return new NfeLine(
                        Value(product, "xProd") ?? string.Empty,
                        barcode is { Length: 13 } && barcode.All(char.IsAsciiDigit) ? barcode : null,
                        decimal.Parse(Value(product, "qCom") ?? "0", CultureInfo.InvariantCulture),
                        decimal.Parse(Value(product, "vUnCom") ?? "0", CultureInfo.InvariantCulture));
                })
                .ToList();
            var issued = Value(ide, "dhEmi") is { } date && DateTimeOffset.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : DateTimeOffset.MinValue;
            var name = Value(emit, "xNome");
            if (name is null || Value(ide, "nNF") is not { } number || lines.Count == 0)
            {
                return null;
            }

            return new Nfe(name, Value(emit, "CNPJ"), number, issued, lines);
        }
        catch (Exception exception) when (exception is FormatException or XmlException or OverflowException)
        {
            return null;
        }
    }
}
