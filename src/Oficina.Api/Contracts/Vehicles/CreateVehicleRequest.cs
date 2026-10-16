namespace Oficina.Api.Contracts.Vehicles;

public sealed record CreateVehicleRequest(string Plate,
    string Brand,
    string Model,
    int Year,
    Guid CustomerId);
