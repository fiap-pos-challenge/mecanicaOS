namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record ServiceOrderBoardPartDto(Guid Id, Guid PartId, string Name, decimal Quantity, string Unit, decimal UnitPrice, decimal Total, bool Reserved, int? LeadTimeDays);
