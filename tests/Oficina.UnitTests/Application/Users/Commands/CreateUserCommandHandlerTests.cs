using NSubstitute;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Users.Commands.CreateUser;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Errors;

namespace Oficina.UnitTests.Application.Users.Commands;

public sealed class CreateUserCommandHandlerTests : UserHandlerFixture
{
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _hasher.Hash(UserTestData.StrongPassword).Returns(UserTestData.PasswordHash);
        _handler = new CreateUserCommandHandler(Users, _hasher, UnitOfWork);
    }

    [Theory]
    [InlineData(Role.Admin)]
    [InlineData(Role.Attendant)]
    [InlineData(Role.Mechanic)]
    public async Task Handle_AnyRole_CreatesTheUserWithTheHashedPassword(Role role)
    {
        var result = await _handler.Handle(Command() with { Role = role }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(role, result.Value.Role);
        Assert.Equal("novo@oficina.com", result.Value.Email);
        Assert.True(result.Value.IsActive);
        Users.Received(1).Add(Arg.Is<User>(user => user.PasswordHash == UserTestData.PasswordHash && user.Role == role));
        await UnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailAlreadyUsed_ReturnsDuplicateEmailWithoutCommit()
    {
        Users.GetByEmailAsync(Email.Create("novo@oficina.com").Value, Arg.Any<CancellationToken>()).Returns(Target);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(IdentityErrors.DuplicateEmail, result.Error);
        Users.DidNotReceive().Add(Arg.Any<User>());
        await AssertNotCommittedAsync();
    }

    [Fact]
    public async Task Handle_InvalidEmail_ReturnsInvalidEmailWithoutCommit()
    {
        var result = await _handler.Handle(Command() with { Email = "invalid" }, CancellationToken.None);

        Assert.Equal(CommonErrors.InvalidEmail, result.Error);
        await AssertNotCommittedAsync();
    }

    [Fact]
    public async Task Handle_InvalidName_ReturnsInvalidNameWithoutCommit()
    {
        var result = await _handler.Handle(Command() with { Name = "A" }, CancellationToken.None);

        Assert.Equal(IdentityErrors.InvalidName, result.Error);
        await AssertNotCommittedAsync();
    }

    [Fact]
    public async Task Handle_CommitFails_ReturnsTheCommitError()
    {
        UnitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.DuplicateRecord));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(CommonErrors.DuplicateRecord, result.Error);
    }

    private static CreateUserCommand Command() => new("Novo Usuário", "Novo@Oficina.com", UserTestData.StrongPassword, Role.Attendant);
}
