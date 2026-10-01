using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.UnitTests.Domain.Common.ValueObjects;

public sealed class MoneyTests
{
    [Theory]
    [InlineData("10", "10.00")]
    [InlineData("10.004", "10.00")]
    [InlineData("10.005", "10.01")]
    [InlineData("10.015", "10.02")]
    [InlineData("0.125", "0.13")]
    [InlineData("0", "0.00")]
    public void Create_WithValidAmount_RoundsHalfUpToTwoDecimals(string input, string expected)
    {
        var result = Money.Create(decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture));

        Assert.True(result.IsSuccess);
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), result.Value.Amount);
    }

    [Fact]
    public void Create_WithNegativeAmount_ReturnsNegativeAmountError()
    {
        var result = Money.Create(-0.01m);

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrors.NegativeAmount, result.Error);
    }

    [Fact]
    public void Add_TwoAmounts_ReturnsSum()
    {
        var sum = Money.Create(10.10m).Value.Add(Money.Create(5.25m).Value);

        Assert.Equal(15.35m, sum.Amount);
    }

    [Fact]
    public void Multiply_ByFractionalQuantity_RoundsHalfUp()
    {
        var price = Money.Create(10.01m).Value;
        var quantity = Quantity.Create(1.5m).Value;

        var total = price.Multiply(quantity);

        Assert.Equal(15.02m, total.Amount);
    }

    [Fact]
    public void Multiply_ByWholeNumber_ReturnsProduct()
    {
        var total = Money.Create(12.35m).Value.Multiply(3);

        Assert.Equal(37.05m, total.Amount);
    }

    [Fact]
    public void Zero_HasZeroAmount()
    {
        Assert.Equal(0m, Money.Zero.Amount);
    }

    [Fact]
    public void Equals_SameAmount_AreEqual()
    {
        Assert.Equal(Money.Create(7m).Value, Money.Create(7.00m).Value);
    }
}
