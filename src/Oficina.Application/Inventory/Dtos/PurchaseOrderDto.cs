namespace Oficina.Application.Inventory.Dtos;

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
