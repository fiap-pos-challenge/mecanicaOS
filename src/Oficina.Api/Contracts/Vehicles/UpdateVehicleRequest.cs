namespace Oficina.Api.Contracts.Vehicles;

public sealed record UpdateVehicleRequest(string Brand, string Model, int Year);
