namespace Oficina.Application.Inventory.Dtos;

public sealed record PurchaseOrderListDto(IReadOnlyList<PurchaseOrderDto> Items);
