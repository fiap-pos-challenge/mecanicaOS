using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.UnitTests.Domain.Customers.ValueObjects;

public sealed class LicensePlateTests
{
    [Theory]
    [InlineData("ABC1234", "ABC1234")]
    [InlineData("abc-1234", "ABC1234")]
    [InlineData(" ABC 1234 ", "ABC1234")]
    public void Create_WithOldFormat_StoresUppercaseWithoutSeparators(string input, string expected)
    {
        var result = LicensePlate.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Value);
    }

    [Theory]
    [InlineData("ABC1D23", "ABC1D23")]
    [InlineData("bra2e19", "BRA2E19")]
    public void Create_WithMercosulFormat_StoresUppercase(string input, string expected)
    {
        var result = LicensePlate.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB12345")]
    [InlineData("ABCD123")]
    [InlineData("ABC12345")]
    [InlineData("ABC1DD3")]
    [InlineData("1234ABC")]
    public void Create_WithInvalidFormat_ReturnsInvalidLicensePlate(string input)
    {
        Assert.Equal(VehicleErrors.InvalidLicensePlate, LicensePlate.Create(input).Error);
    }
}
