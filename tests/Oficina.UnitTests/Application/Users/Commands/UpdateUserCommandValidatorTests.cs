using Oficina.Application.Users.Commands.UpdateUser;
using Oficina.Domain.Identity.Enums;

namespace Oficina.UnitTests.Application.Users.Commands;

public sealed class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        Assert.True(_validator.Validate(new UpdateUserCommand(Guid.CreateVersion7(), "Ana Lima", Role.Attendant)).IsValid);
    }

    [Fact]
    public void Validate_InvalidFields_ReturnOneCatalogErrorEach()
    {
        var errors = _validator.Validate(new UpdateUserCommand(Guid.Empty, "A", (Role)7)).Errors;

        Assert.Equal(new[] { "common.requiredField", "identity.invalidName", "identity.invalidRole" }, errors.Select(error => error.ErrorCode));
    }
}
