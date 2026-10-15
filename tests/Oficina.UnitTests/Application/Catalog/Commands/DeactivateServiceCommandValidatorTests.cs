using Oficina.Application.Catalog.Commands.DeactivateService;

namespace Oficina.UnitTests.Application.Catalog.Commands;

public sealed class DeactivateServiceCommandValidatorTests
{
    private readonly DeactivateServiceCommandValidator _validator = new();

    [Fact]
    public void Validate_WithId_HasNoErrors()
    {
        Assert.True(_validator.Validate(new DeactivateServiceCommand(Guid.CreateVersion7())).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_ReturnsRequiredField()
    {
        Assert.Equal("common.requiredField", Assert.Single(_validator.Validate(new DeactivateServiceCommand(Guid.Empty)).Errors).ErrorCode);
    }
}
