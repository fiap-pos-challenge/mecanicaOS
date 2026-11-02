using Oficina.Domain.Inventory.Enums;

namespace Oficina.Application.Inventory.Dtos;

public sealed record StockMovementDto(
    Guid Id,
    StockMovementType Type,
    decimal Quantity,
    decimal BalanceAfter,
    string Reason,
    Guid? ServiceOrderId,
    Guid? UserId,
    DateTimeOffset CreatedAt);
