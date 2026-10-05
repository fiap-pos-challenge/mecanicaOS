using NSubstitute;
using Oficina.Application.Users.Commands.UpdateUser;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Errors;

namespace Oficina.UnitTests.Application.Users.Commands;

public sealed class UpdateUserCommandHandlerTests : UserHandlerFixture
{
    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserCommandHandlerTests() => _handler = new UpdateUserCommandHandler(Users, CurrentUser, Policy, UnitOfWork);

    [Fact]
    public async Task Handle_ValidCommand_ChangesNameAndRoleAndCommits()
    {
        var result = await _handler.Handle(new UpdateUserCommand(Target.Id, "Ana Souza", Role.Mechanic), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Ana Souza", Target.Name);
        Assert.Equal(Role.Mechanic, Target.Role);
        Assert.Equal(Role.Mechanic, result.Value.Role);
        await UnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AdminKeepingOwnRole_CanRenameThemselves()
    {
        ActAsTarget();

        var result = await _handler.Handle(new UpdateUserCommand(Target.Id, "Ana Souza", Role.Admin), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_AdminDemotingThemselves_IsBlocked()
    {
        ActAsTarget();

        var result = await _handler.Handle(new UpdateUserCommand(Target.Id, "Ana Lima", Role.Attendant), CancellationToken.None);

        Assert.Equal(IdentityErrors.CannotManageOwnAccount, result.Error);
        Assert.Equal(Role.Admin, Target.Role);
        await AssertNotCommittedAsync();
    }

    [Fact]
    public async Task Handle_DemotingTheLastActiveAdmin_IsBlocked()
    {
        Users.CountActiveAdminsAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(new UpdateUserCommand(Target.Id, "Ana Lima", Role.Mechanic), CancellationToken.None);

        Assert.Equal(IdentityErrors.LastActiveAdmin, result.Error);
        await AssertNotCommittedAsync();
    }

    [Fact]
    public async Task Handle_InvalidName_ReturnsInvalidNameWithoutCommit()
    {
        var result = await _handler.Handle(new UpdateUserCommand(Target.Id, "A", Role.Admin), CancellationToken.None);

        Assert.Equal(IdentityErrors.InvalidName, result.Error);
        await AssertNotCommittedAsync();
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound()
    {
        var result = await _handler.Handle(new UpdateUserCommand(Guid.CreateVersion7(), "Ana", Role.Admin), CancellationToken.None);

        Assert.Equal(IdentityErrors.NotFound, result.Error);
        await AssertNotCommittedAsync();
    }

    [Fact]
    public async Task Handle_Anonymous_ReturnsUnauthorized()
    {
        ActAnonymously();

        var result = await _handler.Handle(new UpdateUserCommand(Target.Id, "Ana", Role.Admin), CancellationToken.None);

        Assert.Equal(CommonErrors.Unauthorized, result.Error);
        await AssertNotCommittedAsync();
    }

    [Fact]
    public async Task Handle_CommitFails_ReturnsTheCommitError()
    {
        UnitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.ConcurrencyConflict));

        var result = await _handler.Handle(new UpdateUserCommand(Target.Id, "Ana", Role.Admin), CancellationToken.None);

        Assert.Equal(CommonErrors.ConcurrencyConflict, result.Error);
    }
}
