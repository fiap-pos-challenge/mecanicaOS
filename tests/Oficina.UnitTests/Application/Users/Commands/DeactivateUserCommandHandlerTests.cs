using NSubstitute;
using Oficina.Application.Users.Commands.DeactivateUser;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Errors;

namespace Oficina.UnitTests.Application.Users.Commands;

public sealed class DeactivateUserCommandHandlerTests : UserHandlerFixture
{
    private readonly DeactivateUserCommandHandler _handler;

    public DeactivateUserCommandHandlerTests() => _handler = new DeactivateUserCommandHandler(Users, CurrentUser, Policy, UnitOfWork);

    [Fact]
    public async Task Handle_AnotherAdmin_DeactivatesAndCommits()
    {
        var result = await _handler.Handle(new DeactivateUserCommand(Target.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(Target.IsActive);
        await UnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Self_IsBlocked()
    {
        ActAsTarget();

        var result = await _handler.Handle(new DeactivateUserCommand(Target.Id), CancellationToken.None);

        Assert.Equal(IdentityErrors.CannotManageOwnAccount, result.Error);
        Assert.True(Target.IsActive);
        await AssertNotCommittedAsync();
    }

    [Fact]
    public async Task Handle_LastActiveAdmin_IsBlocked()
    {
        Users.CountActiveAdminsAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(new DeactivateUserCommand(Target.Id), CancellationToken.None);

        Assert.Equal(IdentityErrors.LastActiveAdmin, result.Error);
        await AssertNotCommittedAsync();
    }

    [Fact]
    public async Task Handle_AlreadyInactive_ReturnsAlreadyInactive()
    {
        var inactive = UserTestData.Inactive(Role.Mechanic);
        Users.GetByIdAsync(inactive.Id, Arg.Any<CancellationToken>()).Returns(inactive);

        var result = await _handler.Handle(new DeactivateUserCommand(inactive.Id), CancellationToken.None);

        Assert.Equal(IdentityErrors.AlreadyInactive, result.Error);
        await AssertNotCommittedAsync();
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound()
    {
        var result = await _handler.Handle(new DeactivateUserCommand(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(IdentityErrors.NotFound, result.Error);
        await AssertNotCommittedAsync();
    }

    [Fact]
    public async Task Handle_Anonymous_ReturnsUnauthorized()
    {
        ActAnonymously();

        var result = await _handler.Handle(new DeactivateUserCommand(Target.Id), CancellationToken.None);

        Assert.Equal(CommonErrors.Unauthorized, result.Error);
        await AssertNotCommittedAsync();
    }
}
