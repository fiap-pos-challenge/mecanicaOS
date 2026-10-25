namespace Oficina.Application.Team.Dtos;

public sealed record ProductivityListDto(int PeriodDays, IReadOnlyList<ProductivityDto> Items);
