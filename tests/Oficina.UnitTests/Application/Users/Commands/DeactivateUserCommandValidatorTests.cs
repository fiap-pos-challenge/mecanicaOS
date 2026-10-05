using Oficina.Application.Users.Commands.DeactivateUser;

namespace Oficina.UnitTests.Application.Users.Commands;

public sealed class DeactivateUserCommandValidatorTests
{
    private readonly DeactivateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WithId_HasNoErrors()
    {
        Assert.True(_validator.Validate(new DeactivateUserCommand(Guid.CreateVersion7())).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_ReturnsRequiredField()
    {
        var failure = Assert.Single(_validator.Validate(new DeactivateUserCommand(Guid.Empty)).Errors);

        Assert.Equal("Id", failure.PropertyName);
        Assert.Equal("common.requiredField", failure.ErrorCode);
    }
}
