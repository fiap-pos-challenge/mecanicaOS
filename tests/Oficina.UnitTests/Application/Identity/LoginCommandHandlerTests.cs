using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Commands.Login;
using Oficina.Application.Identity.Dtos;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.UnitTests.Application.Identity;

public sealed class LoginCommandHandlerTests
{
    private const string DummyHash = "$2a$12$dummy";
    private const string WrongPassword = "Wrong@2026";
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExpiresAt = new(2026, 9, 30, 13, 0, 0, TimeSpan.Zero);
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenGenerator _tokens = Substitute.For<ITokenGenerator>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = new(Now);
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _hasher.Verify(UserTestData.StrongPassword, UserTestData.PasswordHash).Returns(true);
        _hasher.DummyHash.Returns(DummyHash);
        _tokens.Generate(Arg.Any<User>()).Returns(new AccessToken("jwt", ExpiresAt));
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new LoginCommandHandler(_users, _hasher, _tokens, _unitOfWork, _clock);
    }

    [Fact]
    public async Task Handle_ActiveUserWithRightPassword_ReturnsTheTokenAndResetsFailures()
    {
        var user = Register(UserTestData.User());
        user.RegisterFailedLogin(Now);

        var result = await _handler.Handle(new LoginCommand(" ANA@oficina.com", UserTestData.StrongPassword), CancellationToken.None);

        Assert.Equal(new LoginResponse("jwt", ExpiresAt), result.Value);
        Assert.Equal(0, user.FailedLoginAttempts);
        _tokens.Received(1).Generate(user);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsInvalidCredentialsAndCountsTheFailure()
    {
        var user = Register(UserTestData.User());

        var result = await _handler.Handle(new LoginCommand("ana@oficina.com", WrongPassword), CancellationToken.None);

        Assert.Equal(IdentityErrors.InvalidCredentials, result.Error);
        Assert.Equal(1, user.FailedLoginAttempts);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        _tokens.DidNotReceive().Generate(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_FifthWrongPassword_LocksTheAccount()
    {
        Register(UserTestData.User());
        Result<LoginResponse>? result = null;

        for (var attempt = 0; attempt < User.MaxFailedLoginAttempts; attempt++)
        {
            result = await _handler.Handle(new LoginCommand("ana@oficina.com", WrongPassword), CancellationToken.None);
        }

        Assert.Equal("identity.accountLocked", result?.Error?.Code);
        Assert.Equal(User.LockoutDuration, result?.Error?.RetryAfter);
    }

    [Fact]
    public async Task Handle_LockedAccount_RejectsEvenTheRightPasswordUntilTheLockoutEnds()
    {
        var user = Register(UserTestData.User());
        for (var attempt = 0; attempt < User.MaxFailedLoginAttempts; attempt++)
        {
            user.RegisterFailedLogin(Now);
        }

        _clock.Advance(TimeSpan.FromMinutes(5));
        var locked = await _handler.Handle(new LoginCommand("ana@oficina.com", UserTestData.StrongPassword), CancellationToken.None);
        _clock.Advance(TimeSpan.FromMinutes(10));
        var released = await _handler.Handle(new LoginCommand("ana@oficina.com", UserTestData.StrongPassword), CancellationToken.None);

        Assert.Equal("identity.accountLocked", locked.Error?.Code);
        Assert.Equal(TimeSpan.FromMinutes(10), locked.Error?.RetryAfter);
        Assert.True(released.IsSuccess);
        Assert.Null(user.LockoutEndAt);
    }

    [Fact]
    public async Task Handle_InactiveUserWithRightPassword_ReturnsAccountDisabled()
    {
        Register(UserTestData.Inactive());

        var result = await _handler.Handle(new LoginCommand("ana@oficina.com", UserTestData.StrongPassword), CancellationToken.None);

        Assert.Equal(IdentityErrors.AccountDisabled, result.Error);
        _hasher.Received(1).Verify(UserTestData.StrongPassword, UserTestData.PasswordHash);
        _tokens.DidNotReceive().Generate(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_InactiveUserWithWrongPassword_ReturnsInvalidCredentials()
    {
        Register(UserTestData.Inactive());

        var result = await _handler.Handle(new LoginCommand("ana@oficina.com", WrongPassword), CancellationToken.None);

        Assert.Equal(IdentityErrors.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task Handle_DeletedUser_ReturnsInvalidCredentials()
    {
        Register(UserTestData.Deleted());

        var result = await _handler.Handle(new LoginCommand("ana@oficina.com", UserTestData.StrongPassword), CancellationToken.None);

        Assert.Equal(IdentityErrors.InvalidCredentials, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_CommitFails_ReturnsTheCommitError(bool rightPassword)
    {
        Register(UserTestData.User());
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.ConcurrencyConflict));

        var result = await _handler.Handle(
            new LoginCommand("ana@oficina.com", rightPassword ? UserTestData.StrongPassword : WrongPassword),
            CancellationToken.None);

        Assert.Equal(CommonErrors.ConcurrencyConflict, result.Error);
        _tokens.DidNotReceive().Generate(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_InvitedUserWithoutPassword_ReturnsInvalidCredentialsWithoutCountingFailures()
    {
        var invited = Register(User.Invite("Ana Lima", Email.Create("ana@oficina.com").Value, Oficina.Domain.Identity.Enums.Role.Mechanic).Value);

        var result = await _handler.Handle(new LoginCommand("ana@oficina.com", UserTestData.StrongPassword), CancellationToken.None);

        Assert.Equal(IdentityErrors.InvalidCredentials, result.Error);
        Assert.Equal(0, invited.FailedLoginAttempts);
        _tokens.DidNotReceive().Generate(Arg.Any<User>());
    }

    private User Register(User user)
    {
        _users.GetByEmailAsync(Email.Create("ana@oficina.com").Value, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }
}
