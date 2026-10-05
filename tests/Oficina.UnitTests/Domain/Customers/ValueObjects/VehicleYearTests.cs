using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.UnitTests.Domain.Customers.ValueObjects;

public sealed class VehicleYearTests
{
    [Theory]
    [InlineData(1900)]
    [InlineData(2020)]
    [InlineData(2050)]
    public void Create_FromFirstAllowedYear_Succeeds(int year)
    {
        var result = VehicleYear.Create(year);

        Assert.True(result.IsSuccess);
        Assert.Equal(year, result.Value.Value);
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(0)]
    [InlineData(-2020)]
    public void Create_BeforeFirstAllowedYear_ReturnsInvalidYear(int year)
    {
        Assert.Equal(VehicleErrors.InvalidYear, VehicleYear.Create(year).Error);
    }
}
