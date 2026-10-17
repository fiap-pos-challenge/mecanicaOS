using Oficina.Domain.ServiceOrders.Enums;

namespace Oficina.Application.Vehicles.Dtos;

public sealed record VehicleOwnerDto(Guid Id, string Name);

public sealed record VehicleServiceOrderSummaryDto(long Number, ServiceOrderStatus Status, DateTimeOffset OpenedAt, DateTimeOffset? CompletedAt);

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
