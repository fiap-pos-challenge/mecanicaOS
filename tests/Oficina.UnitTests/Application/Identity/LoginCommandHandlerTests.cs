using NSubstitute;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Commands.Login;
using Oficina.Application.Identity.Dtos;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.UnitTests.Application.Identity;

public sealed class LoginCommandHandlerTests
{
    private const string DummyHash = "$2a$12$dummy";
    private static readonly DateTimeOffset ExpiresAt = new(2026, 9, 30, 13, 0, 0, TimeSpan.Zero);
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenGenerator _tokens = Substitute.For<ITokenGenerator>();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _hasher.Verify(UserTestData.StrongPassword, UserTestData.PasswordHash).Returns(true);
        _hasher.DummyHash.Returns(DummyHash);
        _tokens.Generate(Arg.Any<User>()).Returns(new AccessToken("jwt", ExpiresAt));
        _handler = new LoginCommandHandler(_users, _hasher, _tokens);
    }

    [Fact]
    public async Task Handle_ActiveUserWithRightPassword_ReturnsTheToken()
    {
        var user = Register(UserTestData.User());

        var result = await _handler.Handle(new LoginCommand(" ANA@oficina.com", UserTestData.StrongPassword), CancellationToken.None);

        Assert.Equal(new LoginResponse("jwt", ExpiresAt), result.Value);
        _tokens.Received(1).Generate(user);
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsInvalidCredentials()
    {
        Register(UserTestData.User());

        var result = await _handler.Handle(new LoginCommand("ana@oficina.com", "Wrong@2026"), CancellationToken.None);

        Assert.Equal(IdentityErrors.InvalidCredentials, result.Error);
        _tokens.DidNotReceive().Generate(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_InactiveUser_ReturnsInvalidCredentials()
    {
        Register(UserTestData.Inactive());

        var result = await _handler.Handle(new LoginCommand("ana@oficina.com", UserTestData.StrongPassword), CancellationToken.None);

        Assert.Equal(IdentityErrors.InvalidCredentials, result.Error);
        _hasher.Received(1).Verify(UserTestData.StrongPassword, UserTestData.PasswordHash);
        _tokens.DidNotReceive().Generate(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_DeletedUser_ReturnsInvalidCredentials()
    {
        Register(UserTestData.Deleted());

        var result = await _handler.Handle(new LoginCommand("ana@oficina.com", UserTestData.StrongPassword), CancellationToken.None);

        Assert.Equal(IdentityErrors.InvalidCredentials, result.Error);
    }

    [Theory]
    [InlineData("desconhecido@oficina.com")]
    [InlineData("invalid")]
    public async Task Handle_UnknownOrInvalidEmail_ReturnsInvalidCredentials(string email)
    {
        var result = await _handler.Handle(new LoginCommand(email, UserTestData.StrongPassword), CancellationToken.None);

        Assert.Equal(IdentityErrors.InvalidCredentials, result.Error);
        _hasher.Received(1).Verify(UserTestData.StrongPassword, DummyHash);
        _tokens.DidNotReceive().Generate(Arg.Any<User>());
    }

    private User Register(User user)
    {
        _users.GetByEmailAsync(Email.Create("ana@oficina.com").Value, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }
}
