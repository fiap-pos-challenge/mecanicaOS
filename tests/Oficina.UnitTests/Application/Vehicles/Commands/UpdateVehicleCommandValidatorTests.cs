using Oficina.Application.Vehicles.Commands.UpdateVehicle;

namespace Oficina.UnitTests.Application.Vehicles.Commands;

public class UpdateVehicleCommandValidatorTests
{
    private readonly UpdateVehicleCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidParams_ReturnsNoErrors()
    {
        Assert.True(_validator.Validate(Command()).IsValid);
    }

    [Theory]
    [InlineData("HB20", "", 2025, "vehicle.invalidModel")]
    [InlineData("", "Hyundai", 2025, "vehicle.invalidBrand")]
    [InlineData("HB20", "Hyundai", 1800, "vehicle.invalidYear")]
    public void Validate_InvalidParams_ReturnsError(string brand, string model, int year, string code)
    {
        var failure = Assert.Single(_validator.Validate(Command() with { Brand = brand, Model = model, Year = year }).Errors);
        Assert.Equal(code, failure.ErrorCode);
    }

    [Fact]
    public void Validate_NegativeMileage_ReturnsInvalidMileage()
    {
        var failure = Assert.Single(_validator.Validate(Command() with { Mileage = -5 }).Errors);
        Assert.Equal(Oficina.Domain.Customers.Errors.VehicleErrors.InvalidMileage.Code, failure.ErrorCode);
    }

    private static UpdateVehicleCommand Command() =>
        new UpdateVehicleCommand(Guid.NewGuid(),
            VehicleTestData.model,
            VehicleTestData.brand,
            VehicleTestData.VehicleYear().Value,
            50_000);
}
