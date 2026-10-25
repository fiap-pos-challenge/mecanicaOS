namespace Oficina.Application.Inventory.Dtos;

public sealed record StockCountListDto(IReadOnlyList<StockCountDto> Items);
