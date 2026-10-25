using Oficina.Application.Users.Commands.CreateUser;
using Oficina.Domain.Identity.Enums;

namespace Oficina.UnitTests.Application.Users.Commands;

public sealed class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        Assert.True(_validator.Validate(Command()).IsValid);
    }

    [Theory]
    [InlineData("short1!A")]
    [InlineData("Oficina@2026")]
    [InlineData("Uma Senha Longa 1!")]
    public void Validate_StrongPassword_HasNoErrors(string password)
    {
        Assert.True(_validator.Validate(Command() with { Password = password }).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ab1!")]
    [InlineData("oficina@2026")]
    [InlineData("OFICINA@2026")]
    [InlineData("Oficina@abcd")]
    [InlineData("Oficina2026")]
    [InlineData("Oficina@2026Oficina@2026Oficina@2026Oficina@2026Oficina@2026Oficin")]
    public void Validate_WeakPassword_ReturnsWeakPassword(string password)
    {
        var failure = Assert.Single(_validator.Validate(Command() with { Password = password }).Errors);

        Assert.Equal("Password", failure.PropertyName);
        Assert.Equal("identity.weakPassword", failure.ErrorCode);
    }

    [Theory]
    [InlineData("Name", "identity.invalidName")]
    [InlineData("Email", "common.invalidEmail")]
    [InlineData("EmptyEmail", "common.invalidEmail")]
    [InlineData("Role", "identity.invalidRole")]
    public void Validate_InvalidField_ReturnsTheCatalogCode(string field, string code)
    {
        var command = field switch
        {
            "Name" => Command() with { Name = " " },
            "Email" => Command() with { Email = "invalid" },
            "EmptyEmail" => Command() with { Email = string.Empty },
            _ => Command() with { Role = (Role)99 },
        };

        Assert.Equal(code, Assert.Single(_validator.Validate(command).Errors).ErrorCode);
    }

    private static CreateUserCommand Command() => new("Novo Usuário", "novo@oficina.com", UserTestData.StrongPassword, Role.Mechanic);
}
