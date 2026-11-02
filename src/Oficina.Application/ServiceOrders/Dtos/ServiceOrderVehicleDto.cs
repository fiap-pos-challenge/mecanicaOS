namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record ServiceOrderVehicleDto(Guid Id, string LicensePlate, string Brand, string Model, int Year);
