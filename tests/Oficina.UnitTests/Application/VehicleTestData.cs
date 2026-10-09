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
        TimeProvider.System.GetLocalNow()).Value;
}
