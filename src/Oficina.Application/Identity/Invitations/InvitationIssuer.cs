using Oficina.Application.Abstractions.Notifications;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Identity.Invitations;

public sealed class InvitationIssuer(IUserInvitationRepository invitations, IEmailSender emailSender, TimeProvider timeProvider)
{
    public async Task<IssuedInvitation> IssueAsync(User user, Guid? createdBy, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        foreach (var open in await invitations.ListOpenAsync(user.Id, cancellationToken))
        {
            open.Revoke(now);
        }

        var token = InvitationToken.Generate();
        var invitation = UserInvitation.Issue(user.Id, InvitationToken.Hash(token), createdBy, now);
        invitations.Add(invitation);
        return new IssuedInvitation(invitation, token);
    }

    public Task SendAsync(User user, IssuedInvitation issued, CancellationToken cancellationToken) =>
        emailSender.SendInvitationAsync(
            new InvitationMessage(user.Name, user.Email.Value, issued.Token, issued.Invitation.ExpiresAt),
            cancellationToken);
}
