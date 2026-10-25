namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record MechanicOrderListDto(IReadOnlyList<MechanicOrderDto> Items);
