using Oficina.Application.Catalog.Commands.ReactivateService;

namespace Oficina.UnitTests.Application.Catalog.Commands;

public sealed class ReactivateServiceCommandValidatorTests
{
    private readonly ReactivateServiceCommandValidator _validator = new();

    [Fact]
    public void Validate_WithId_HasNoErrors()
    {
        Assert.True(_validator.Validate(new ReactivateServiceCommand(Guid.CreateVersion7())).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_ReturnsRequiredField()
    {
        Assert.Equal("common.requiredField", Assert.Single(_validator.Validate(new ReactivateServiceCommand(Guid.Empty)).Errors).ErrorCode);
    }
}
