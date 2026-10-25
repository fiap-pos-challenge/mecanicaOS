using Oficina.Application.Inventory.Dtos;

namespace Oficina.Api.Contracts.Inventory;

public sealed record PurchaseOrderRequest(Guid SupplierId, IReadOnlyList<PurchaseOrderLineInput>? Lines);
