using NSubstitute;
using Oficina.Application.Users.Commands.ReactivateUser;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Errors;

namespace Oficina.UnitTests.Application.Users.Commands;

public sealed class ReactivateUserCommandHandlerTests : UserHandlerFixture
{
    private readonly ReactivateUserCommandHandler _handler;

    public ReactivateUserCommandHandlerTests() => _handler = new ReactivateUserCommandHandler(Users, UnitOfWork);

    [Fact]
    public async Task Handle_InactiveUser_ReactivatesAndCommits()
    {
        var inactive = UserTestData.Inactive(Role.Attendant);
        Users.GetByIdAsync(inactive.Id, Arg.Any<CancellationToken>()).Returns(inactive);

        var result = await _handler.Handle(new ReactivateUserCommand(inactive.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(inactive.IsActive);
        await UnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ActiveUser_ReturnsAlreadyActive()
    {
        var result = await _handler.Handle(new ReactivateUserCommand(Target.Id), CancellationToken.None);

        Assert.Equal(IdentityErrors.AlreadyActive, result.Error);
        await AssertNotCommittedAsync();
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound()
    {
        var result = await _handler.Handle(new ReactivateUserCommand(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(IdentityErrors.NotFound, result.Error);
        await AssertNotCommittedAsync();
    }
}
