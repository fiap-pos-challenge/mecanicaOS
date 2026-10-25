namespace Oficina.Application.Inventory.Dtos;

public sealed record StockMovementListDto(StockItemDto Item, IReadOnlyList<StockMovementDto> Items);
