using Oficina.Application.ServiceOrders.Queries.ListServiceOrders;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.UnitTests.Application.ServiceOrders.Queries;

public sealed class ListServiceOrdersQueryValidatorTests
{
    private readonly ListServiceOrdersQueryValidator _validator = new();

    [Theory]
    [InlineData(null, null)]
    [InlineData("Received", null)]
    [InlineData("delivered", "AAA9999")]
    public void Validate_ValidFilters_ReturnsNoErrors(string? status, string? plate)
    {
        Assert.True(_validator.Validate(new ListServiceOrdersQuery(1, 20, status, plate)).IsValid);
    }

    [Fact]
    public void Validate_InvalidPage_ReturnsInvalidPage()
    {
        var failure = Assert.Single(_validator.Validate(new ListServiceOrdersQuery(0, 20, null, null)).Errors);
        Assert.Equal(CommonErrors.InvalidPage.Code, failure.ErrorCode);
    }

    [Fact]
    public void Validate_PageSizeAboveMaximum_ReturnsInvalidPageSize()
    {
        var failure = Assert.Single(_validator.Validate(new ListServiceOrdersQuery(1, 101, null, null)).Errors);
        Assert.Equal(CommonErrors.InvalidPageSize.Code, failure.ErrorCode);
    }

    [Theory]
    [InlineData("Cancelled")]
    [InlineData("3")]
    public void Validate_UnknownStatus_ReturnsInvalidStatus(string status)
    {
        var failure = Assert.Single(_validator.Validate(new ListServiceOrdersQuery(1, 20, status, null)).Errors);
        Assert.Equal(ServiceOrderErrors.InvalidStatus.Code, failure.ErrorCode);
    }

    [Fact]
    public void Validate_InvalidPlate_ReturnsInvalidLicensePlate()
    {
        var failure = Assert.Single(_validator.Validate(new ListServiceOrdersQuery(1, 20, null, "invalid")).Errors);
        Assert.Equal(VehicleErrors.InvalidLicensePlate.Code, failure.ErrorCode);
    }
}
