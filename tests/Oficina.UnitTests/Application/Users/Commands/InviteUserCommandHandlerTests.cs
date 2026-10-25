using NSubstitute;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Identity.Invitations;
using Oficina.Application.Users.Commands.InviteUser;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Errors;
using Oficina.UnitTests.Application.Identity;

namespace Oficina.UnitTests.Application.Users.Commands;

public sealed class InviteUserCommandHandlerTests : InvitationFixture
{
    private InviteUserCommandHandler Handler => new(Users, Issuer, CurrentUser, UnitOfWork);

    [Fact]
    public async Task Handle_NewEmail_CreatesThePendingUserStoresOnlyTheTokenHashAndSendsTheLink()
    {
        UserInvitation? stored = null;
        Invitations.Add(Arg.Do<UserInvitation>(invitation => stored = invitation));
        InvitationMessage? sent = null;
        await EmailSender.SendInvitationAsync(Arg.Do<InvitationMessage>(message => sent = message), Arg.Any<CancellationToken>());

        var result = await Handler.Handle(new InviteUserCommand("Bruna Oliveira", "Bruna@Oficina.com", Role.Manager), CancellationToken.None);

        Assert.Equal(UserStatus.InvitationPending, result.Value.Status);
        Assert.Equal("bruna@oficina.com", result.Value.Email);
        Assert.Equal(Now.AddHours(72), result.Value.InvitationExpiresAt);
        Users.Received(1).Add(Arg.Is<User>(user => user.PasswordHash == null && user.Role == Role.Manager));
        Assert.NotNull(stored);
        Assert.NotNull(sent);
        Assert.Equal(AdminId, stored.CreatedBy);
        Assert.Equal(InvitationToken.Hash(sent.Token), stored.TokenHash);
        Assert.NotEqual(sent.Token, stored.TokenHash);
        Assert.Equal("bruna@oficina.com", sent.RecipientEmail);
        Assert.Equal("Bruna Oliveira", sent.RecipientName);
    }

    [Fact]
    public async Task Handle_EmailAlreadyUsed_ReturnsDuplicateWithoutSending()
    {
        Users.GetByEmailAsync(Email.Create("rafael@oficina.com").Value, Arg.Any<CancellationToken>()).Returns(Invited);

        var result = await Handler.Handle(new InviteUserCommand("Outro", "rafael@oficina.com", Role.Mechanic), CancellationToken.None);

        Assert.Equal(IdentityErrors.DuplicateEmail, result.Error);
        await EmailSender.DidNotReceive().SendInvitationAsync(Arg.Any<InvitationMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidEmailOrName_FailsWithoutCommit()
    {
        Assert.Equal(CommonErrors.InvalidEmail, (await Handler.Handle(new InviteUserCommand("Outro", "x", Role.Mechanic), CancellationToken.None)).Error);
        Assert.Equal(IdentityErrors.InvalidName, (await Handler.Handle(new InviteUserCommand("O", "o@oficina.com", Role.Mechanic), CancellationToken.None)).Error);
        await UnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CommitFails_DoesNotSendTheEmail()
    {
        UnitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.DuplicateRecord));

        var result = await Handler.Handle(new InviteUserCommand("Outro", "o@oficina.com", Role.Mechanic), CancellationToken.None);

        Assert.Equal(CommonErrors.DuplicateRecord, result.Error);
        await EmailSender.DidNotReceive().SendInvitationAsync(Arg.Any<InvitationMessage>(), Arg.Any<CancellationToken>());
    }
}
