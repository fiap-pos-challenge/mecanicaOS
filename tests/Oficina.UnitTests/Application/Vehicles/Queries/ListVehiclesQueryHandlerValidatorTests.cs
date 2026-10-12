using Oficina.Application.Vehicles.Queries.ListVehicles;

namespace Oficina.UnitTests.Application.Vehicles.Queries;

public class ListVehiclesQueryHandlerValidatorTests
{

    private readonly ListVehicleQueryValidator _validator = new ListVehicleQueryValidator();

    [Fact]
    public void Validate_ReceivesValidData_ReturnsNoErrors()
    {
        ListVehiclesQuery query = new ListVehiclesQuery(1, 10);
        var result = _validator.Validate(query);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ReceivesInValidPageData_ReturnsInvalidPageError()
    {
        ListVehiclesQuery query = new ListVehiclesQuery(-1, 10);
        var result = _validator.Validate(query);
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);;
    }

    [Fact]
    public void Validate_ReceivesInValidPageSizeData_ReturnsInvalidPageSizeError()
    {
        ListVehiclesQuery query = new ListVehiclesQuery(1, -2);
        var result = _validator.Validate(query);
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);;
    }
}
