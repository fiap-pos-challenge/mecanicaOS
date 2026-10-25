using System.Globalization;
using System.Text;
using Oficina.Application.Abstractions.Payments;
using QRCoder;

namespace Oficina.Infrastructure.Payments;

public sealed class PixCodes : IPixCodes
{
    public string Payload(string key, string merchantName, string city, decimal amount, string txId)
    {
        var account = Field("00", "br.gov.bcb.pix") + Field("01", key.Trim());
        var payload = new StringBuilder()
            .Append(Field("00", "01"))
            .Append(Field("26", account))
            .Append(Field("52", "0000"))
            .Append(Field("53", "986"))
            .Append(Field("54", amount.ToString("0.00", CultureInfo.InvariantCulture)))
            .Append(Field("58", "BR"))
            .Append(Field("59", Ascii(merchantName, 25)))
            .Append(Field("60", Ascii(city, 15)))
            .Append(Field("62", Field("05", txId)))
            .Append("6304")
            .ToString();
        return payload + Crc16(payload).ToString("X4", CultureInfo.InvariantCulture);
    }

    public string QrCodeDataUrl(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data).GetGraphic(6);
        return $"data:image/png;base64,{Convert.ToBase64String(png)}";
    }

    private static string Field(string id, string value) => $"{id}{value.Length.ToString("00", CultureInfo.InvariantCulture)}{value}";

    private static string Ascii(string value, int max)
    {
        var normalized = new string(value.Normalize(NormalizationForm.FormD).Where(character => character < 128 && !char.IsControl(character)).ToArray()).Trim().ToUpperInvariant();
        var text = normalized.Length == 0 ? "OFICINA" : normalized;
        return text.Length > max ? text[..max] : text;
    }

    private static ushort Crc16(string value)
    {
        ushort crc = 0xFFFF;
        foreach (var data in Encoding.ASCII.GetBytes(value))
        {
            crc ^= (ushort)(data << 8);
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
            }
        }

        return crc;
    }
}
