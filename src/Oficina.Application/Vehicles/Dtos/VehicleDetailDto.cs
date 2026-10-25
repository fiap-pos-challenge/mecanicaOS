namespace Oficina.Application.Vehicles.Dtos;

public sealed record VehicleDetailDto(
    Guid Id,
    string Model,
    string Brand,
    string LicensePlate,
    int Year,
    int Mileage,
    VehicleOwnerDto Owner,
    IReadOnlyList<VehicleServiceOrderSummaryDto> ServiceOrders,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
