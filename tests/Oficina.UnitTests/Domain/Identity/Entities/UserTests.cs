using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Errors;

namespace Oficina.UnitTests.Domain.Identity.Entities;

public sealed class UserTests
{
    private const string PasswordHash = "$2a$12$abcdefghijklmnopqrstuuSomeBcryptHashValue0123456789abcd";
    private static readonly Email UserEmail = Email.Create("admin@oficina.com").Value;

    [Theory]
    [InlineData(Role.Admin)]
    [InlineData(Role.Attendant)]
    [InlineData(Role.Mechanic)]
    public void Create_ForAnyRole_IsActiveAndCanLogin(Role role)
    {
        var result = User.Create(" Ana Lima ", UserEmail, PasswordHash, role);

        Assert.True(result.IsSuccess);
        var user = result.Value;
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("Ana Lima", user.Name);
        Assert.Equal(UserEmail, user.Email);
        Assert.Equal(PasswordHash, user.PasswordHash);
        Assert.Equal(role, user.Role);
        Assert.True(user.IsActive);
        Assert.True(user.CanLogin);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Create_WithTooShortName_ReturnsInvalidName(string name)
    {
        Assert.Equal(IdentityErrors.InvalidName, User.Create(name, UserEmail, PasswordHash, Role.Admin).Error);
    }

    [Fact]
    public void Create_WithNameAbove120Characters_ReturnsInvalidName()
    {
        Assert.Equal(IdentityErrors.InvalidName, User.Create(new string('N', 121), UserEmail, PasswordHash, Role.Admin).Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutPasswordHash_ReturnsInvalidPasswordHash(string hash)
    {
        Assert.Equal(IdentityErrors.InvalidPasswordHash, User.Create("Ana Lima", UserEmail, hash, Role.Admin).Error);
    }

    [Fact]
    public void Create_WithPasswordHashAbove255Characters_ReturnsInvalidPasswordHash()
    {
        Assert.Equal(IdentityErrors.InvalidPasswordHash, User.Create("Ana Lima", UserEmail, new string('h', 256), Role.Admin).Error);
    }

    [Fact]
    public void ChangeName_WithValidName_UpdatesName()
    {
        var user = CreateUser(Role.Attendant);

        var result = user.ChangeName("Ana Paula Lima");

        Assert.True(result.IsSuccess);
        Assert.Equal("Ana Paula Lima", user.Name);
    }

    [Fact]
    public void ChangeName_WithInvalidName_KeepsPreviousName()
    {
        var user = CreateUser(Role.Attendant);

        Assert.Equal(IdentityErrors.InvalidName, user.ChangeName("").Error);
        Assert.Equal("Ana Lima", user.Name);
    }

    [Fact]
    public void ChangeRole_ReplacesRole()
    {
        var user = CreateUser(Role.Mechanic);

        user.ChangeRole(Role.Admin);

        Assert.Equal(Role.Admin, user.Role);
    }

    [Fact]
    public void Deactivate_ActiveUser_BlocksLogin()
    {
        var user = CreateUser(Role.Mechanic);

        var result = user.Deactivate();

        Assert.True(result.IsSuccess);
        Assert.False(user.IsActive);
        Assert.False(user.CanLogin);
    }

    [Fact]
    public void Deactivate_InactiveUser_ReturnsAlreadyInactive()
    {
        var user = CreateUser(Role.Mechanic);
        user.Deactivate();

        Assert.Equal(IdentityErrors.AlreadyInactive, user.Deactivate().Error);
    }

    [Fact]
    public void Reactivate_InactiveUser_AllowsLoginAgain()
    {
        var user = CreateUser(Role.Mechanic);
        user.Deactivate();

        var result = user.Reactivate();

        Assert.True(result.IsSuccess);
        Assert.True(user.CanLogin);
    }

    [Fact]
    public void Reactivate_ActiveUser_ReturnsAlreadyActive()
    {
        Assert.Equal(IdentityErrors.AlreadyActive, CreateUser(Role.Mechanic).Reactivate().Error);
    }

    [Fact]
    public void Delete_BlocksLoginImmediately()
    {
        var user = CreateUser(Role.Attendant);

        user.Delete();

        Assert.False(user.IsActive);
        Assert.False(user.CanLogin);
    }

    private static User CreateUser(Role role) => User.Create("Ana Lima", UserEmail, PasswordHash, role).Value;
}
