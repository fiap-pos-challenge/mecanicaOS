using Oficina.Application.Vehicles.Queries.ListVehicles;

namespace Oficina.UnitTests.Application.Vehicles.Queries;

public sealed class ListVehiclesQueryValidatorTests
{
    private readonly ListVehiclesQueryValidator _validator = new();

    [Fact]
    public void Validate_ReceivesValidData_ReturnsNoErrors()
    {
        Assert.True(_validator.Validate(new ListVehiclesQuery(1, 10)).IsValid);
    }

    [Fact]
    public void Validate_ReceivesInValidPageData_ReturnsInvalidPageError()
    {
        Assert.Single(_validator.Validate(new ListVehiclesQuery(-1, 10)).Errors);
    }

    [Fact]
    public void Validate_ReceivesInValidPageSizeData_ReturnsInvalidPageSizeError()
    {
        Assert.Single(_validator.Validate(new ListVehiclesQuery(1, -2)).Errors);
    }
}
