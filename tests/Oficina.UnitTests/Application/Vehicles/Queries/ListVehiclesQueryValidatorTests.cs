using Oficina.Application.Vehicles.Queries.ListVehicles;
using Oficina.Domain.Customers.Errors;

namespace Oficina.UnitTests.Application.Vehicles.Queries;

public sealed class ListVehiclesQueryValidatorTests
{
    private readonly ListVehiclesQueryValidator _validator = new();

    [Fact]
    public void Validate_ReceivesValidData_ReturnsNoErrors()
    {
        Assert.True(_validator.Validate(new ListVehiclesQuery(1, 10, null)).IsValid);
    }

    [Fact]
    public void Validate_ReceivesValidPlate_ReturnsNoErrors()
    {
        Assert.True(_validator.Validate(new ListVehiclesQuery(1, 10, VehicleTestData.LicensePlate().Value)).IsValid);
    }

    [Fact]
    public void Validate_ReceivesInValidPageData_ReturnsInvalidPageError()
    {
        var result = _validator.Validate(new ListVehiclesQuery(-1, 10, null));
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Validate_ReceivesInValidPageSizeData_ReturnsInvalidPageSizeError()
    {
        var result = _validator.Validate(new ListVehiclesQuery(1, -2, null));
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Validate_ReceivesInvalidPlate_ReturnsInvalidLicensePlateError()
    {
        var failure = Assert.Single(_validator.Validate(new ListVehiclesQuery(1, 10, "invalid")).Errors);
        Assert.Equal(VehicleErrors.InvalidLicensePlate.Code, failure.ErrorCode);
    }
}
