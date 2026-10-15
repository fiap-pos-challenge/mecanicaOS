namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record ServiceOrderCustomerDto(Guid Id, string Name, string Document, string? Email);
