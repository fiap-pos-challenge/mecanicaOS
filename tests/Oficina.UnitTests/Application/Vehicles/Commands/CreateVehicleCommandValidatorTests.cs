using Oficina.Application.Vehicles.Commands.CreateVehicle;
using Oficina.Domain.Customers.Errors;

namespace Oficina.UnitTests.Application.Vehicles.Commands;

public sealed class CreateVehicleCommandValidatorTests
{

    private readonly CreateVehicleCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ReturnsSuccess()
    {
        Assert.True(_validator.Validate(Command()).IsValid);
    }

    [Fact]
    public void Validate_InvalidPlate_ReturnsInvalidPlateError()
    {
        var failure = Assert.Single(_validator.Validate(Command() with { Plate = "111-BBBB" }).Errors);
        Assert.Equal(failure.ErrorCode, VehicleErrors.InvalidLicensePlate.Code);
    }

    [Fact]
    public void Validate_InvalidVehicleYear_ReturnsInvalidVehicleYearError()
    {
        var failure = Assert.Single(_validator.Validate(Command() with { Year = 1800 }).Errors);
        Assert.Equal(failure.ErrorCode, VehicleErrors.InvalidYear.Code);
    }

    [Fact]
    public void Validate_EmptyBrand_ReturnsInvalidBrandError()
    {
        var failure = Assert.Single(_validator.Validate(Command() with { Brand = string.Empty }).Errors);
        Assert.Equal(failure.ErrorCode, VehicleErrors.InvalidBrand.Code);
    }

    [Fact]
    public void Validate_EmptyModel_ReturnsInvalidModelError()
    {
        var failure = Assert.Single(_validator.Validate(Command() with { Model = string.Empty }).Errors);
        Assert.Equal(failure.ErrorCode, VehicleErrors.InvalidModel.Code);
    }


    [Theory]
    [InlineData(-1)]
    [InlineData(10_000_000)]
    public void Validate_MileageOutOfRange_ReturnsInvalidMileage(int mileage)
    {
        var failure = Assert.Single(_validator.Validate(Command() with { Mileage = mileage }).Errors);
        Assert.Equal(VehicleErrors.InvalidMileage.Code, failure.ErrorCode);
    }

    private static CreateVehicleCommand Command() =>
        new CreateVehicleCommand(Guid.NewGuid(),
            VehicleTestData.LicensePlate().Value,
            VehicleTestData.brand,
            VehicleTestData.model,
            VehicleTestData.VehicleYear().Value,
            42_000);

}
