namespace Oficina.Application.Inventory.Dtos;

public sealed record StockOverviewDto(StockKpisDto Kpis, IReadOnlyList<StockItemDto> Items, int Page, int PageSize, int TotalItems, IReadOnlyList<string> Locations);
