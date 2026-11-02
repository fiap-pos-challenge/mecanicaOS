using NSubstitute;
using Oficina.Application.Users.Commands.DeleteUser;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Errors;

namespace Oficina.UnitTests.Application.Users.Commands;

public sealed class DeleteUserCommandHandlerTests : UserHandlerFixture
{
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests() => _handler = new DeleteUserCommandHandler(Users, CurrentUser, Policy, UnitOfWork);

    [Fact]
    public async Task Handle_AnotherAdmin_DeactivatesRemovesAndCommits()
    {
        var result = await _handler.Handle(new DeleteUserCommand(Target.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(Target.IsActive);
        Users.Received(1).Remove(Target);
        await UnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Self_IsBlocked()
    {
        ActAsTarget();

        var result = await _handler.Handle(new DeleteUserCommand(Target.Id), CancellationToken.None);

        Assert.Equal(IdentityErrors.CannotManageOwnAccount, result.Error);
        Users.DidNotReceive().Remove(Arg.Any<User>());
        await AssertNotCommittedAsync();
    }

    [Fact]
    public async Task Handle_LastActiveAdmin_IsBlocked()
    {
        Users.CountActiveAdminsAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(new DeleteUserCommand(Target.Id), CancellationToken.None);

        Assert.Equal(IdentityErrors.LastActiveAdmin, result.Error);
        Users.DidNotReceive().Remove(Arg.Any<User>());
        await AssertNotCommittedAsync();
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound()
    {
        var result = await _handler.Handle(new DeleteUserCommand(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(IdentityErrors.NotFound, result.Error);
        await AssertNotCommittedAsync();
    }

    [Fact]
    public async Task Handle_Anonymous_ReturnsUnauthorized()
    {
        ActAnonymously();

        var result = await _handler.Handle(new DeleteUserCommand(Target.Id), CancellationToken.None);

        Assert.Equal(CommonErrors.Unauthorized, result.Error);
        await AssertNotCommittedAsync();
    }
}
