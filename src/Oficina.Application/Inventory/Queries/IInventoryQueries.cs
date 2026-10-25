using Oficina.Application.Common.Pagination;
using Oficina.Application.Inventory.Dtos;

namespace Oficina.Application.Inventory.Queries;

public interface IInventoryQueries
{
    Task<StockOverviewDto> OverviewAsync(PageRequest page, string? term, string? situation, string? location, CancellationToken cancellationToken);

    Task<StockMovementListDto?> MovementsAsync(Guid partId, CancellationToken cancellationToken);

    Task<StockMovementDto?> MovementAsync(Guid movementId, CancellationToken cancellationToken);

    Task<StockItemDto?> ByBarcodeAsync(string barcode, CancellationToken cancellationToken);

    Task<StockCountListDto> CountsAsync(CancellationToken cancellationToken);

    Task<StockCountDto?> CountAsync(Guid id, CancellationToken cancellationToken);

    Task<SupplierListDto> SuppliersAsync(CancellationToken cancellationToken);

    Task<PurchaseSuggestionListDto> SuggestionsAsync(CancellationToken cancellationToken);

    Task<PurchaseOrderListDto> OrdersAsync(CancellationToken cancellationToken);

    Task<PurchaseOrderDto?> OrderAsync(Guid id, CancellationToken cancellationToken);
}
