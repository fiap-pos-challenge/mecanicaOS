namespace Oficina.Application.Reports.Dtos;

public sealed record ReportResultDto(IReadOnlyList<ReportRowDto> Rows, string Unit, decimal? Total, string From, string To);
