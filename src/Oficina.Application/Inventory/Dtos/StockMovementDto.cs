namespace Oficina.Application.Inventory.Dtos;

public sealed record StockMovementDto(Guid Id, string Type, decimal Quantity, decimal BalanceAfter, string Reason, string UserName, long? ServiceOrderNumber, DateTimeOffset OccurredAt);
