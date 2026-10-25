using NSubstitute;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Commands.AcceptInvitation;
using Oficina.Application.Identity.Invitations;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Errors;

namespace Oficina.UnitTests.Application.Identity;

public sealed class AcceptInvitationCommandHandlerTests : InvitationFixture
{
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();

    public AcceptInvitationCommandHandlerTests() => _hasher.Hash(UserTestData.StrongPassword).Returns(UserTestData.PasswordHash);

    private AcceptInvitationCommandHandler Handler => new(Invitations, Users, _hasher, UnitOfWork, Clock);

    [Fact]
    public async Task Handle_OpenInvitation_DefinesThePasswordAndConsumesTheToken()
    {
        var invitation = OpenInvitation();

        var result = await Handler.Handle(new AcceptInvitationCommand(Token, UserTestData.StrongPassword), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserTestData.PasswordHash, Invited.PasswordHash);
        Assert.Equal(Now, invitation.AcceptedAt);
        await UnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsNotFoundWithoutCommit()
    {
        var result = await Handler.Handle(new AcceptInvitationCommand("x", UserTestData.StrongPassword), CancellationToken.None);

        Assert.Equal(IdentityErrors.InvitationNotFound, result.Error);
        await UnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserMissing_ReturnsNotFound()
    {
        Store(UserInvitation.Issue(Guid.CreateVersion7(), InvitationToken.Hash(Token), AdminId, Now));

        Assert.Equal(IdentityErrors.InvitationNotFound, (await Handler.Handle(new AcceptInvitationCommand(Token, UserTestData.StrongPassword), CancellationToken.None)).Error);
    }

    [Fact]
    public async Task Handle_ExpiredInvitation_KeepsTheUserWithoutPassword()
    {
        Store(UserInvitation.Issue(Invited.Id, InvitationToken.Hash(Token), AdminId, Now.AddDays(-4)));

        var result = await Handler.Handle(new AcceptInvitationCommand(Token, UserTestData.StrongPassword), CancellationToken.None);

        Assert.Equal(IdentityErrors.InvitationExpired, result.Error);
        Assert.Null(Invited.PasswordHash);
    }

    [Fact]
    public async Task Handle_UserAlreadyHasPassword_ReturnsAlreadyUsed()
    {
        OpenInvitation();
        Invited.DefinePassword("$2a$12$other");

        Assert.Equal(IdentityErrors.InvitationAlreadyUsed, (await Handler.Handle(new AcceptInvitationCommand(Token, UserTestData.StrongPassword), CancellationToken.None)).Error);
    }

    [Fact]
    public async Task Handle_CommitFails_ReturnsTheError()
    {
        OpenInvitation();
        UnitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.DuplicateRecord));

        Assert.Equal(CommonErrors.DuplicateRecord, (await Handler.Handle(new AcceptInvitationCommand(Token, UserTestData.StrongPassword), CancellationToken.None)).Error);
    }
}
