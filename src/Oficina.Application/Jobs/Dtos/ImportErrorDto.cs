namespace Oficina.Application.Jobs.Dtos;

public sealed record ImportErrorDto(int Line, string Field, string Message);
