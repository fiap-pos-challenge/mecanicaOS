namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record ServiceOrderPartItemDto(Guid Id, Guid PartId, string Name, decimal Quantity, decimal UnitPrice, decimal Total);
