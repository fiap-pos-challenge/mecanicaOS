using Oficina.Application.Catalog.Commands.CreateService;

namespace Oficina.UnitTests.Application.Catalog.Commands;

public sealed class CreateServiceCommandValidatorTests
{
    private readonly CreateServiceCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        Assert.True(_validator.Validate(Command()).IsValid);
    }

    [Fact]
    public void Validate_WithoutDescription_HasNoErrors()
    {
        Assert.True(_validator.Validate(Command() with { Description = null }).IsValid);
    }

    [Theory]
    [InlineData("Name", "catalog.invalidName")]
    [InlineData("BasePrice", "catalog.invalidBasePrice")]
    [InlineData("EstimatedMinutes", "catalog.invalidEstimatedMinutes")]
    [InlineData("CommissionPercent", "catalog.invalidCommissionPercent")]
    [InlineData("WarrantyDays", "catalog.invalidWarrantyDays")]
    public void Validate_InvalidField_ReturnsTheCatalogCode(string field, string code)
    {
        var command = field switch
        {
            "Name" => Command() with { Name = new string('a', 121) },
            "BasePrice" => Command() with { BasePrice = 0m },
            "CommissionPercent" => Command() with { CommissionPercent = -1m },
            "WarrantyDays" => Command() with { WarrantyDays = 3651 },
            _ => Command() with { EstimatedMinutes = 0 },
        };

        var failure = Assert.Single(_validator.Validate(command).Errors);

        Assert.Equal(field, failure.PropertyName);
        Assert.Equal(code, failure.ErrorCode);
    }

    [Theory]
    [InlineData("-10")]
    [InlineData("0.004")]
    public void Validate_PriceThatIsNotPositiveMoney_ReturnsInvalidBasePrice(string price)
    {
        var command = Command() with { BasePrice = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture) };

        Assert.Equal("catalog.invalidBasePrice", Assert.Single(_validator.Validate(command).Errors).ErrorCode);
    }

    private static CreateServiceCommand Command() => new("Troca de óleo", "Inclui filtro", 150m, 45, 10m, 90, []);
}
