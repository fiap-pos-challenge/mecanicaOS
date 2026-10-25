namespace Oficina.Application.Jobs.Dtos;

public sealed record ServiceHistoryDto(IReadOnlyList<ServiceHistoryEntryDto> Items);
