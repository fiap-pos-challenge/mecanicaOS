using NSubstitute;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Identity.Invitations;
using Oficina.Application.Users.Commands.ResendInvitation;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Errors;
using Oficina.UnitTests.Application.Identity;

namespace Oficina.UnitTests.Application.Users.Commands;

public sealed class ResendInvitationCommandHandlerTests : InvitationFixture
{
    private ResendInvitationCommandHandler Handler => new(Users, Issuer, CurrentUser, UnitOfWork);

    [Fact]
    public async Task Handle_PendingUser_RevokesTheOpenInvitationAndSendsANewOne()
    {
        var previous = UserInvitation.Issue(Invited.Id, InvitationToken.Hash("antigo"), AdminId, Now.AddHours(-2));
        Invitations.ListOpenAsync(Invited.Id, Arg.Any<CancellationToken>()).Returns(new[] { previous });

        var result = await Handler.Handle(new ResendInvitationCommand(Invited.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Now, previous.RevokedAt);
        Invitations.Received(1).Add(Arg.Is<UserInvitation>(invitation => invitation.UserId == Invited.Id && invitation.IsOpen));
        await EmailSender.Received(1).SendInvitationAsync(Arg.Any<InvitationMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound() =>
        Assert.Equal(IdentityErrors.NotFound, (await Handler.Handle(new ResendInvitationCommand(Guid.CreateVersion7()), CancellationToken.None)).Error);

    [Fact]
    public async Task Handle_UserWithPassword_ReturnsNotPending()
    {
        Invited.DefinePassword("$2a$12$hash");

        Assert.Equal(IdentityErrors.InvitationNotPending, (await Handler.Handle(new ResendInvitationCommand(Invited.Id), CancellationToken.None)).Error);
    }

    [Fact]
    public async Task Handle_CommitFails_DoesNotSend()
    {
        UnitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.DuplicateRecord));

        Assert.Equal(CommonErrors.DuplicateRecord, (await Handler.Handle(new ResendInvitationCommand(Invited.Id), CancellationToken.None)).Error);
        await EmailSender.DidNotReceive().SendInvitationAsync(Arg.Any<InvitationMessage>(), Arg.Any<CancellationToken>());
    }
}
