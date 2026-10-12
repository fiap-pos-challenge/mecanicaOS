namespace Oficina.Application.Vehicles.Dtos;

public sealed record VehicleDto(
    Guid Id,
    string Model,
    string Brand,
    string LicensePlate,
    int Year,
    Guid CustomerId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
