namespace Oficina.Application.Jobs.Dtos;

public sealed record ImportReport(int TotalLines, int ValidLines, IReadOnlyList<ImportErrorDto> Errors);
