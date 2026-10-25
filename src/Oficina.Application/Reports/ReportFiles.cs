using System.IO.Compression;
using System.Security;
using System.Text;
using Oficina.Application.Documents;
using Oficina.Application.Reports.Dtos;

namespace Oficina.Application.Reports;

public static class ReportFiles
{
    public const string XlsxType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static byte[] Xlsx(string title, ReportResultDto result)
    {
        var rows = new StringBuilder();
        rows.Append(Row(1, [Text(title), Text(string.Empty)]));
        rows.Append(Row(2, [Text("Grupo"), Text("Valor")]));
        var index = 3;
        foreach (var row in result.Rows)
        {
            rows.Append(Row(index++, [Text(row.Label), Number(result.Unit == "money" ? row.Value / 100 : row.Value)]));
        }

        if (result.Total is { } total)
        {
            rows.Append(Row(index, [Text("Total"), Number(result.Unit == "money" ? total / 100 : total)]));
        }

        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            Entry(zip, "[Content_Types].xml", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>""");
            Entry(zip, "_rels/.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
            Entry(zip, "xl/workbook.xml", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Relatorio" sheetId="1" r:id="rId1"/></sheets></workbook>""");
            Entry(zip, "xl/_rels/workbook.xml.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>""");
            Entry(zip, "xl/worksheets/sheet1.xml", $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>{rows}</sheetData></worksheet>""");
        }

        return output.ToArray();
    }

    public static byte[] Pdf(string title, ReportResultDto result)
    {
        var lines = new List<string> { title, $"Período: {result.From} a {result.To}", string.Empty };
        lines.AddRange(result.Rows.Select(row => $"{row.Label}: {ReportEngine.Format(result.Unit, row.Value)}"));
        if (result.Total is { } total)
        {
            lines.Add(string.Empty);
            lines.Add($"Total: {ReportEngine.Format(result.Unit, total)}");
        }

        return PdfWriter.Write(lines);
    }

    public static string Html(string title, ReportResultDto result)
    {
        var body = new StringBuilder($"<h2>{SecurityElement.Escape(title)}</h2><p>Período: {result.From} a {result.To}</p><table>");
        foreach (var row in result.Rows)
        {
            body.Append($"<tr><td>{SecurityElement.Escape(row.Label)}</td><td>{SecurityElement.Escape(ReportEngine.Format(result.Unit, row.Value))}</td></tr>");
        }

        if (result.Total is { } total)
        {
            body.Append($"<tr><td><strong>Total</strong></td><td><strong>{SecurityElement.Escape(ReportEngine.Format(result.Unit, total))}</strong></td></tr>");
        }

        return body.Append("</table>").ToString();
    }

    private static string Row(int index, string[] cells) => $"<row r=\"{index}\">{string.Concat(cells)}</row>";

    private static string Text(string value) => $"<c t=\"inlineStr\"><is><t>{SecurityElement.Escape(value)}</t></is></c>";

    private static string Number(decimal value) => $"<c><v>{value.ToString(System.Globalization.CultureInfo.InvariantCulture)}</v></c>";

    private static void Entry(ZipArchive zip, string name, string content)
    {
        using var stream = zip.CreateEntry(name, CompressionLevel.Fastest).Open();
        stream.Write(Encoding.UTF8.GetBytes(content));
    }
}
