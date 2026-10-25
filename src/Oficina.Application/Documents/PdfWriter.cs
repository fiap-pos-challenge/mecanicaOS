using System.Globalization;
using System.Text;

namespace Oficina.Application.Documents;

public static class PdfWriter
{
    public static byte[] Write(IReadOnlyList<string> lines)
    {
        var content = new StringBuilder("BT /F1 11 Tf 50 800 Td 14 TL\n");
        foreach (var line in lines)
        {
            content.Append(CultureInfo.InvariantCulture, $"({Escape(line)}) Tj T*\n");
        }

        content.Append("ET");
        var stream = Encoding.Latin1.GetBytes(content.ToString());
        var objects = new List<byte[]>
        {
            Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"),
            Encoding.ASCII.GetBytes("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            Encoding.ASCII.GetBytes("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>"),
            Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"),
            Concat(Encoding.ASCII.GetBytes($"<< /Length {stream.Length} >>\nstream\n"), stream, Encoding.ASCII.GetBytes("\nendstream")),
        };
        using var output = new MemoryStream();
        void Raw(string text) => output.Write(Encoding.ASCII.GetBytes(text));
        Raw("%PDF-1.4\n");
        var offsets = new List<long>();
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(output.Position);
            Raw($"{index + 1} 0 obj\n");
            output.Write(objects[index]);
            Raw("\nendobj\n");
        }

        var xref = output.Position;
        Raw($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            Raw($"{offset.ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n");
        }

        Raw($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%EOF\n");
        return output.ToArray();
    }

    private static byte[] Concat(byte[] first, byte[] second, byte[] third) => [.. first, .. second, .. third];

    private static string Escape(string value) =>
        new string(value.Select(character => character > 255 ? '?' : character).ToArray()).Replace("\\", "\\\\", StringComparison.Ordinal).Replace("(", "\\(", StringComparison.Ordinal).Replace(")", "\\)", StringComparison.Ordinal);
}
