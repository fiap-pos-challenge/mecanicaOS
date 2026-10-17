using Oficina.Application.Catalog.Commands.UpdateService;

namespace Oficina.UnitTests.Application.Catalog.Commands;

public sealed class UpdateServiceCommandValidatorTests
{
    private readonly UpdateServiceCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        Assert.True(_validator.Validate(Command()).IsValid);
    }

    [Theory]
    [InlineData("Id", "common.requiredField")]
    [InlineData("Name", "catalog.invalidName")]
    [InlineData("BasePrice", "catalog.invalidBasePrice")]
    [InlineData("EstimatedMinutes", "catalog.invalidEstimatedMinutes")]
    public void Validate_InvalidField_ReturnsTheCatalogCode(string field, string code)
    {
        var command = field switch
        {
            "Id" => Command() with { Id = Guid.Empty },
            "Name" => Command() with { Name = " " },
            "BasePrice" => Command() with { BasePrice = -5m },
            _ => Command() with { EstimatedMinutes = -1 },
        };

        var failure = Assert.Single(_validator.Validate(command).Errors);

        Assert.Equal(field, failure.PropertyName);
        Assert.Equal(code, failure.ErrorCode);
    }

    private static UpdateServiceCommand Command() => new(Guid.CreateVersion7(), "Alinhamento", null, 90m, 30, 10m, 90, []);
}
