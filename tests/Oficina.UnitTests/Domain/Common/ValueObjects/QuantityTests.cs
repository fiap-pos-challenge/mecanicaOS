using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.UnitTests.Domain.Common.ValueObjects;

public sealed class QuantityTests
{
    [Theory]
    [InlineData("1")]
    [InlineData("0.001")]
    [InlineData("2.5")]
    public void Create_WithPositiveValue_Succeeds(string input)
    {
        var value = decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture);

        var result = Quantity.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value.Value);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("0.0001")]
    public void Create_WithZeroNegativeOrTooPrecise_ReturnsInvalidQuantityError(string input)
    {
        var result = Quantity.Create(decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture));

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrors.InvalidQuantity, result.Error);
    }

    [Fact]
    public void Add_TwoQuantities_ReturnsSum()
    {
        var sum = Quantity.Create(1.5m).Value.Add(Quantity.Create(2m).Value);

        Assert.Equal(3.5m, sum.Value);
    }
}
