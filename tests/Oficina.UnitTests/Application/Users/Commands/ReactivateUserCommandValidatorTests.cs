using Oficina.Application.Users.Commands.ReactivateUser;

namespace Oficina.UnitTests.Application.Users.Commands;

public sealed class ReactivateUserCommandValidatorTests
{
    private readonly ReactivateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WithId_HasNoErrors()
    {
        Assert.True(_validator.Validate(new ReactivateUserCommand(Guid.CreateVersion7())).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_ReturnsRequiredField()
    {
        var failure = Assert.Single(_validator.Validate(new ReactivateUserCommand(Guid.Empty)).Errors);

        Assert.Equal("Id", failure.PropertyName);
        Assert.Equal("common.requiredField", failure.ErrorCode);
    }
}
