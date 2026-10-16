using Oficina.Application.Identity.Invitations;
using Oficina.Application.Identity.Queries.InspectInvitation;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Errors;

namespace Oficina.UnitTests.Application.Identity;

public sealed class InspectInvitationQueryHandlerTests : InvitationFixture
{
    private InspectInvitationQueryHandler Handler => new(Invitations, Users, Clock);

    [Fact]
    public async Task Handle_OpenInvitation_ReturnsTheNameMaskedEmailRoleAndExpiry()
    {
        var invitation = OpenInvitation();

        var result = await Handler.Handle(new InspectInvitationQuery(Token), CancellationToken.None);

        Assert.Equal("Rafael Costa", result.Value.Name);
        Assert.Equal("r•••@oficina.com", result.Value.MaskedEmail);
        Assert.Equal(Role.Mechanic, result.Value.Role);
        Assert.Equal(invitation.ExpiresAt, result.Value.ExpiresAt);
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsNotFound() =>
        Assert.Equal(IdentityErrors.InvitationNotFound, (await Handler.Handle(new InspectInvitationQuery("x"), CancellationToken.None)).Error);

    [Fact]
    public async Task Handle_ExpiredInvitation_ReturnsExpired()
    {
        Store(UserInvitation.Issue(Invited.Id, InvitationToken.Hash(Token), AdminId, Now.AddDays(-4)));

        Assert.Equal(IdentityErrors.InvitationExpired, (await Handler.Handle(new InspectInvitationQuery(Token), CancellationToken.None)).Error);
    }

    [Fact]
    public async Task Handle_UserAlreadyDefinedThePassword_ReturnsNotFound()
    {
        OpenInvitation();
        Invited.DefinePassword("$2a$12$hash");

        Assert.Equal(IdentityErrors.InvitationNotFound, (await Handler.Handle(new InspectInvitationQuery(Token), CancellationToken.None)).Error);
    }

    [Fact]
    public async Task Handle_UserMissing_ReturnsNotFound()
    {
        Store(UserInvitation.Issue(Guid.CreateVersion7(), InvitationToken.Hash(Token), AdminId, Now));

        Assert.Equal(IdentityErrors.InvitationNotFound, (await Handler.Handle(new InspectInvitationQuery(Token), CancellationToken.None)).Error);
    }
}
