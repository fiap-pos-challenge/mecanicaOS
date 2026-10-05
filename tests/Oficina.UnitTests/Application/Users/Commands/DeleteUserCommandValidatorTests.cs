using Oficina.Application.Users.Commands.DeleteUser;

namespace Oficina.UnitTests.Application.Users.Commands;

public sealed class DeleteUserCommandValidatorTests
{
    private readonly DeleteUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WithId_HasNoErrors()
    {
        Assert.True(_validator.Validate(new DeleteUserCommand(Guid.CreateVersion7())).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_ReturnsRequiredField()
    {
        var failure = Assert.Single(_validator.Validate(new DeleteUserCommand(Guid.Empty)).Errors);

        Assert.Equal("Id", failure.PropertyName);
        Assert.Equal("common.requiredField", failure.ErrorCode);
    }
}
