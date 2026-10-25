namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record ServiceOrderServiceItemDto(Guid Id, Guid ServiceId, string Name, int Quantity, decimal UnitPrice, decimal Total);
