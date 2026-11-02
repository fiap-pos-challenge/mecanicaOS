using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.UnitTests.Domain.Customers.Entities;

public sealed class VehicleTests
{
    private static readonly DateTimeOffset Today = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid CustomerId = Guid.CreateVersion7();
    private static readonly LicensePlate Plate = LicensePlate.Create("BRA2E19").Value;
    private static readonly VehicleYear Year = VehicleYear.Create(2020).Value;

    [Fact]
    public void Create_WithValidData_SetsFields()
    {
        var result = Vehicle.Create(CustomerId, Plate, " Volkswagen ", " Gol ", Year, Today);

        Assert.True(result.IsSuccess);
        var vehicle = result.Value;
        Assert.NotEqual(Guid.Empty, vehicle.Id);
        Assert.Equal(CustomerId, vehicle.CustomerId);
        Assert.Equal(Plate, vehicle.LicensePlate);
        Assert.Equal("Volkswagen", vehicle.Brand);
        Assert.Equal("Gol", vehicle.Model);
        Assert.Equal(Year, vehicle.Year);
    }

    [Fact]
    public void Create_WithEmptyCustomer_ReturnsCustomerRequired()
    {
        Assert.Equal(VehicleErrors.CustomerRequired, Vehicle.Create(Guid.Empty, Plate, "Volkswagen", "Gol", Year, Today).Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_WithEmptyBrand_ReturnsInvalidBrand(string brand)
    {
        Assert.Equal(VehicleErrors.InvalidBrand, Vehicle.Create(CustomerId, Plate, brand, "Gol", Year, Today).Error);
    }

    [Fact]
    public void Create_WithBrandAbove60Characters_ReturnsInvalidBrand()
    {
        Assert.Equal(VehicleErrors.InvalidBrand, Vehicle.Create(CustomerId, Plate, new string('B', 61), "Gol", Year, Today).Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_WithEmptyModel_ReturnsInvalidModel(string model)
    {
        Assert.Equal(VehicleErrors.InvalidModel, Vehicle.Create(CustomerId, Plate, "Volkswagen", model, Year, Today).Error);
    }

    [Fact]
    public void ChangeDetails_WithValidData_UpdatesBrandModelAndYear_KeepingPlate()
    {
        var vehicle = Vehicle.Create(CustomerId, Plate, "Volkswagen", "Gol", Year, Today).Value;
        var newYear = VehicleYear.Create(2021).Value;

        var result = vehicle.ChangeDetails("Fiat", "Uno", newYear, Today);

        Assert.True(result.IsSuccess);
        Assert.Equal("Fiat", vehicle.Brand);
        Assert.Equal("Uno", vehicle.Model);
        Assert.Equal(newYear, vehicle.Year);
        Assert.Equal(Plate, vehicle.LicensePlate);
    }

    [Fact]
    public void ChangeDetails_WithInvalidModel_KeepsPreviousData()
    {
        var vehicle = Vehicle.Create(CustomerId, Plate, "Volkswagen", "Gol", Year, Today).Value;

        var result = vehicle.ChangeDetails("Fiat", "", Year, Today);

        Assert.Equal(VehicleErrors.InvalidModel, result.Error);
        Assert.Equal("Volkswagen", vehicle.Brand);
        Assert.Equal("Gol", vehicle.Model);
    }

    [Theory]
    [InlineData(2026)]
    [InlineData(2027)]
    public void Create_UpToNextYear_Succeeds(int year)
    {
        Assert.True(Vehicle.Create(CustomerId, Plate, "Volkswagen", "Gol", VehicleYear.Create(year).Value, Today).IsSuccess);
    }

    [Fact]
    public void Create_AfterNextYear_ReturnsInvalidYear()
    {
        Assert.Equal(VehicleErrors.InvalidYear, Vehicle.Create(CustomerId, Plate, "Volkswagen", "Gol", VehicleYear.Create(2028).Value, Today).Error);
    }

    [Fact]
    public void ChangeDetails_ToYearAfterNextYear_ReturnsInvalidYearAndKeepsData()
    {
        var vehicle = Vehicle.Create(CustomerId, Plate, "Volkswagen", "Gol", Year, Today).Value;

        var result = vehicle.ChangeDetails("Fiat", "Uno", VehicleYear.Create(2030).Value, Today);

        Assert.Equal(VehicleErrors.InvalidYear, result.Error);
        Assert.Equal(Year, vehicle.Year);
        Assert.Equal("Volkswagen", vehicle.Brand);
    }
}
