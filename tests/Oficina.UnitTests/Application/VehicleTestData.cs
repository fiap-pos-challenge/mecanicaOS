using Oficina.Application.Vehicles.Dtos;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.UnitTests.Application;

public static class VehicleTestData
{
    public const string brand = "Fiat";
    public const string model = "Uno";

    public static LicensePlate LicensePlate() => Oficina.Domain.Customers.ValueObjects.LicensePlate.Create("AAA9999").Value;
    public static VehicleYear VehicleYear() => Oficina.Domain.Customers.ValueObjects.VehicleYear.Create(2026).Value;

    public static Vehicle Vehicle() => Oficina.Domain.Customers.Entities.Vehicle.Create(
        CustomerTestData.Customer().Id,
        LicensePlate(),
        brand,
        model,
        VehicleYear(),
        42_000,
        TimeProvider.System.GetLocalNow()).Value;

    public static VehicleDetailDto VehicleDetail()
    {
        var vehicle = Vehicle();
        return new VehicleDetailDto(
            vehicle.Id,
            vehicle.Model,
            vehicle.Brand,
            vehicle.LicensePlate.Value,
            vehicle.Year.Value,
            vehicle.Mileage,
            new VehicleOwnerDto(vehicle.CustomerId, CustomerTestData.Customer().Name),
            new List<VehicleServiceOrderSummaryDto>(),
            TimeProvider.System.GetUtcNow(),
            null);
    }
}
