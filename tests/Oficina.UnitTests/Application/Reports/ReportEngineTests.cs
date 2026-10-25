using System.IO.Compression;
using System.Text;
using Oficina.Application.Analytics;
using Oficina.Application.Documents;
using Oficina.Application.Reports;
using Oficina.Application.Reports.Dtos;

namespace Oficina.UnitTests.Application.Reports;

public sealed class ReportEngineTests
{
    private static readonly DateTimeOffset Delivered = new(2026, 10, 10, 15, 0, 0, TimeSpan.Zero);

    private static ReportDefinitionDto Definition(string measure, string groupBy, string chart = "column", string preset = "thisMonth", string? from = null, string? to = null) =>
        new(measure, groupBy, new ReportPeriodDto(preset, from, to), chart);

    private static readonly IReadOnlyList<SaleFact> Sales =
    [
        new(Guid.CreateVersion7(), 1, Delivered, "Maria", "Marcos", "Pix", 300m,
            [new SaleServiceLine("Alinhamento", 100m, 10m)],
            [new SalePartLine("Pastilha", "Peças", 2m, 200m, 100m)]),
        new(Guid.CreateVersion7(), 2, Delivered.AddDays(2), "João", "Paula", "Balcão", 100m,
            [new SaleServiceLine("Alinhamento", 100m, 12m)],
            []),
    ];

    [Theory]
    [InlineData("commission", "paymentMethod", "column")]
    [InlineData("revenue", "customer", "line")]
    [InlineData("revenue", "month", "donut")]
    [InlineData("margin", "service", "donut")]
    public void Problem_RejectsCombinationsThatDoNotMakeSense(string measure, string groupBy, string chart) =>
        Assert.NotNull(ReportEngine.Problem(Definition(measure, groupBy, chart)));

    [Theory]
    [InlineData(null, "2026-01-10")]
    [InlineData("2026-02-10", "2026-01-10")]
    [InlineData("2025-01-01", "2026-06-01")]
    public void Problem_ValidatesTheCustomPeriod(string? from, string to) =>
        Assert.NotNull(ReportEngine.Problem(Definition("revenue", "month", preset: "custom", from: from, to: to)));

    [Fact]
    public void IsReadable_ChecksEveryField()
    {
        Assert.True(ReportEngine.IsReadable(Definition("revenue", "month")));
        Assert.False(ReportEngine.IsReadable(Definition("lucro", "month")));
        Assert.False(ReportEngine.IsReadable(null));
        Assert.Null(ReportEngine.Problem(Definition("revenue", "service", "donut")));
    }

    [Theory]
    [InlineData("thisMonth", "2026-10-01", "2026-10-17")]
    [InlineData("lastMonth", "2026-09-01", "2026-09-30")]
    [InlineData("lastQuarter", "2026-07-01", "2026-09-30")]
    [InlineData("yearToDate", "2026-01-01", "2026-10-17")]
    public void Range_ComputesThePresets(string preset, string from, string to)
    {
        var (start, end) = ReportEngine.Range(new ReportPeriodDto(preset, null, null), new DateOnly(2026, 10, 17));

        Assert.Equal(DateOnly.Parse(from, System.Globalization.CultureInfo.InvariantCulture), start);
        Assert.Equal(DateOnly.Parse(to, System.Globalization.CultureInfo.InvariantCulture), end);
    }

    [Fact]
    public void Run_GroupsRevenueByMonthWithEmptyMonths()
    {
        var result = ReportEngine.Run(Definition("revenue", "month"), Sales, new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 31), TimeSpan.FromHours(-3));

        Assert.Equal(["set/26", "out/26"], result.Rows.Select(row => row.Label));
        Assert.Equal(0m, result.Rows[0].Value);
        Assert.Equal(40_000m, result.Rows[1].Value);
        Assert.Equal(40_000m, result.Total);
        Assert.Equal("money", result.Unit);
    }

    [Fact]
    public void Run_ComputesMarginTicketPartsAndCommission()
    {
        var from = new DateOnly(2026, 10, 1);
        var to = new DateOnly(2026, 10, 31);
        var offset = TimeSpan.FromHours(-3);

        var margin = ReportEngine.Run(Definition("margin", "mechanic", "table"), Sales, from, to, offset);
        Assert.Equal(66.7m, margin.Rows.Single(row => row.Label == "Marcos").Value);
        Assert.Null(margin.Total);

        var ticket = ReportEngine.Run(Definition("averageTicket", "paymentMethod", "table"), Sales, from, to, offset);
        Assert.Equal(30_000m, ticket.Rows.Single(row => row.Label == "Pix").Value);

        var parts = ReportEngine.Run(Definition("partsOut", "category"), Sales, from, to, offset);
        Assert.Equal(2m, parts.Rows.Single(row => row.Label == "Peças").Value);

        var commission = ReportEngine.Run(Definition("commission", "service", "donut"), Sales, from, to, offset);
        Assert.Equal(2_200m, commission.Rows.Single().Value);

        var orders = ReportEngine.Run(Definition("orderCount", "week", "line"), Sales, from, to, offset);
        Assert.Equal(2m, orders.Total);
        Assert.StartsWith("Sem. ", orders.Rows[0].Label, StringComparison.Ordinal);
    }

    [Fact]
    public void Explain_AndFormat_AreInPortuguese()
    {
        Assert.Equal("Ticket médio por mecânico, último trimestre.", ReportEngine.Explain(Definition("averageTicket", "mechanic", preset: "lastQuarter")));
        Assert.Contains("1.234,56", ReportEngine.Format("money", 123_456m), StringComparison.Ordinal);
        Assert.Equal("12,5%", ReportEngine.Format("percent", 12.5m));
        Assert.Equal("3", ReportEngine.Format("count", 3m));
    }

    [Fact]
    public void Files_BuildAValidXlsxPdfAndHtml()
    {
        var result = ReportEngine.Run(Definition("revenue", "service"), Sales, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), TimeSpan.Zero);

        using var zip = new ZipArchive(new MemoryStream(ReportFiles.Xlsx("Faturamento <teste>", result)));
        using var sheet = new StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var xml = sheet.ReadToEnd();
        Assert.Contains("Faturamento &lt;teste&gt;", xml, StringComparison.Ordinal);
        Assert.Contains("<v>200</v>", xml, StringComparison.Ordinal);

        Assert.StartsWith("%PDF-1.4", Encoding.ASCII.GetString(ReportFiles.Pdf("Faturamento", result)), StringComparison.Ordinal);
        Assert.Contains("<strong>Total</strong>", ReportFiles.Html("Faturamento", result), StringComparison.Ordinal);
    }

    [Fact]
    public void PdfWriter_EscapesTextAndWritesTheCrossReference()
    {
        var pdf = Encoding.Latin1.GetString(PdfWriter.Write(["Orçamento (teste) \\ ok", "→ seta"]));

        Assert.Contains("Orçamento \\(teste\\) \\\\ ok", pdf, StringComparison.Ordinal);
        Assert.Contains("? seta", pdf, StringComparison.Ordinal);
        Assert.Contains("startxref", pdf, StringComparison.Ordinal);
        Assert.EndsWith("%EOF\n", pdf, StringComparison.Ordinal);
    }
}
