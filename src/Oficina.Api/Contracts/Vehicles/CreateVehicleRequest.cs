namespace Oficina.Api.Contracts.Vehicles;

public sealed record CreateVehicleRequest(Guid CustomerId, string LicensePlate, string Brand, string Model, int Year, int Mileage);
