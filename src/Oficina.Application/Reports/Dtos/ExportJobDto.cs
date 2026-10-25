namespace Oficina.Application.Reports.Dtos;

public sealed record ExportJobDto(Guid Id, string Format, string Status, bool Large, string? FileUrl, string? FileName);
