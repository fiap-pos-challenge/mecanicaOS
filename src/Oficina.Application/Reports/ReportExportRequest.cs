using Oficina.Application.Reports.Dtos;

namespace Oficina.Application.Reports;

public sealed record ReportExportRequest(ReportDefinitionDto Definition, string Format)
{
    public const string Kind = "ReportExport";
    public const string Xlsx = "xlsx";
    public const string Pdf = "pdf";

    public static bool IsFormat(string? format) => format is Xlsx or Pdf;
}
