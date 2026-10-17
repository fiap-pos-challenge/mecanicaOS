namespace Oficina.Application.Inventory.Dtos;

public sealed record StockItemDto(
    Guid PartId,
    string Code,
    string Name,
    string Type,
    string UnitOfMeasure,
    string Location,
    decimal StockQuantity,
    decimal MinimumStock,
    decimal Outflow30d,
    decimal UnitCost,
    DateTimeOffset? LastOutflowAt,
    string? Barcode,
    string Situation);

public sealed record StockKpisDto(
    int BelowMinimum,
    decimal SuggestedOrderTotal,
    decimal OutflowMonth,
    decimal? OutflowMonthDeltaPercent,
    decimal? TurnoverDays,
    int TurnoverGoalDays,
    int IdleItems,
    decimal IdleValue,
    decimal StockValue,
    int TotalItems);

public sealed record StockOverviewDto(StockKpisDto Kpis, IReadOnlyList<StockItemDto> Items, int Page, int PageSize, int TotalItems, IReadOnlyList<string> Locations);

public sealed record StockMovementDto(Guid Id, string Type, decimal Quantity, decimal BalanceAfter, string Reason, string UserName, long? ServiceOrderNumber, DateTimeOffset OccurredAt);

public sealed record StockMovementListDto(StockItemDto Item, IReadOnlyList<StockMovementDto> Items);

public sealed record InvoiceLineDto(string Description, string? Barcode, Guid? PartId, decimal Quantity, decimal UnitCost);

public sealed record InvoiceDraftDto(string SupplierName, string? SupplierDocument, Guid? SupplierId, string InvoiceNumber, DateTimeOffset IssuedAt, IReadOnlyList<InvoiceLineDto> Lines);

public sealed record InvoiceConfirmLine(Guid PartId, decimal Quantity, decimal UnitCost);

public sealed record InvoiceConfirmedDto(int Lines);

public sealed record CountLineDto(Guid PartId, string Code, string Name, string Type, string UnitOfMeasure, decimal SystemQuantity, decimal? CountedQuantity);

public sealed record StockCountDto(
    Guid Id,
    string Location,
    string Status,
    string OpenedBy,
    DateTimeOffset OpenedAt,
    DateTimeOffset? SubmittedAt,
    string? DecidedBy,
    DateTimeOffset? DecidedAt,
    string? RejectionReason,
    IReadOnlyList<CountLineDto> Lines);

public sealed record StockCountListDto(IReadOnlyList<StockCountDto> Items);

public sealed record CountLineInput(Guid PartId, decimal? CountedQuantity);

public sealed record SupplierDto(Guid Id, string Name, string? Document, string? Email, string? Phone, int LeadTimeDays, bool IsActive);

public sealed record SupplierListDto(IReadOnlyList<SupplierDto> Items);

public sealed record PurchaseSuggestionDto(
    Guid PartId,
    string Code,
    string Name,
    string Type,
    string UnitOfMeasure,
    Guid? SupplierId,
    decimal StockQuantity,
    decimal MinimumStock,
    decimal Forecast30d,
    decimal SuggestedQuantity,
    decimal UnitCost);

public sealed record PurchaseSuggestionListDto(IReadOnlyList<PurchaseSuggestionDto> Items);

public sealed record PurchaseOrderLineDto(Guid PartId, string Code, string Name, decimal Quantity, decimal UnitCost);

public sealed record PurchaseOrderDto(
    Guid Id,
    long Number,
    Guid SupplierId,
    string SupplierName,
    string Status,
    decimal Total,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt,
    IReadOnlyList<PurchaseOrderLineDto> Lines);

public sealed record PurchaseOrderListDto(IReadOnlyList<PurchaseOrderDto> Items);

public sealed record PurchaseOrderLineInput(Guid PartId, decimal Quantity, decimal UnitCost);
