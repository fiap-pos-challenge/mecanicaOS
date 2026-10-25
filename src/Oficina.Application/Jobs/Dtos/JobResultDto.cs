namespace Oficina.Application.Jobs.Dtos;

public sealed record JobResultDto(string? FileName, string? Content, int? TotalLines, int? ValidLines, IReadOnlyList<ImportErrorDto>? Errors);
