namespace Oficina.Application.Vehicles.Dto;

public sealed record VehicleDto(
    Guid Id,
    string Model,
    string Brand,
    string LicensePlate,
    int Year,
    Guid CustomerId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
