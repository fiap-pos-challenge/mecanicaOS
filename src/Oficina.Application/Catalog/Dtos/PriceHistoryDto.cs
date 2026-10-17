namespace Oficina.Application.Catalog.Dtos;

public sealed record PriceHistoryDto(IReadOnlyList<PriceChangeDto> Items);
