using System.Globalization;
using Oficina.Infrastructure.Payments;

namespace Oficina.FunctionalTests.Payments;

public sealed class PixCodesTests
{
    private readonly PixCodes _pix = new();

    [Fact]
    public void Payload_FollowsTheBrCodeLayout_WithAValidCrc()
    {
        var payload = _pix.Payload(" pix@oficina.com ", "Auto Center São Paulo Ltda Matriz", "São Paulo", 150.5m, "ABCDEF0123456789ABCDEF012");

        Assert.StartsWith("000201", payload, StringComparison.Ordinal);
        Assert.Contains("0014br.gov.bcb.pix0115pix@oficina.com", payload, StringComparison.Ordinal);
        Assert.Contains("5406150.50", payload, StringComparison.Ordinal);
        Assert.Contains("5925AUTO CENTER SAO PAULO LT", payload, StringComparison.Ordinal);
        Assert.Contains("6009SAO PAULO", payload, StringComparison.Ordinal);
        Assert.Contains("62290525ABCDEF0123456789ABCDEF012", payload, StringComparison.Ordinal);
        var body = payload[..^4];
        Assert.EndsWith("6304", body, StringComparison.Ordinal);
        Assert.Equal(Crc(body), payload[^4..]);
    }

    [Fact]
    public void Payload_UsesADefaultNameWhenItIsBlank()
    {
        Assert.Contains("5907OFICINA", _pix.Payload("chave", "   ", "Rio", 1m, "TX"), StringComparison.Ordinal);
    }

    [Fact]
    public void QrCode_IsAPngDataUrl()
    {
        var url = _pix.QrCodeDataUrl("000201");

        Assert.StartsWith("data:image/png;base64,iVBORw0KGgo", url, StringComparison.Ordinal);
    }

    private static string Crc(string value)
    {
        ushort crc = 0xFFFF;
        foreach (var data in System.Text.Encoding.ASCII.GetBytes(value))
        {
            crc ^= (ushort)(data << 8);
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
            }
        }

        return crc.ToString("X4", CultureInfo.InvariantCulture);
    }
}
