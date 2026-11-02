using Oficina.Application.Identity.Commands.Login;

namespace Oficina.UnitTests.Application.Identity;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_FilledFields_HasNoErrors()
    {
        Assert.True(_validator.Validate(new LoginCommand("ana@oficina.com", "qualquer")).IsValid);
    }

    [Fact]
    public void Validate_EmptyFields_ReturnsRequiredFieldForEach()
    {
        var errors = _validator.Validate(new LoginCommand(string.Empty, string.Empty)).Errors;

        Assert.Equal(new[] { "Email", "Password" }, errors.Select(error => error.PropertyName));
        Assert.All(errors, error => Assert.Equal("common.requiredField", error.ErrorCode));
    }
}
