using MediatR;
using Oficina.Application.Common.Privacy;
using Oficina.Application.Identity.Dtos;
using Oficina.Application.Identity.Invitations;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Identity.Queries.InspectInvitation;

public sealed class InspectInvitationQueryHandler(
    IUserInvitationRepository invitations,
    IUserRepository users,
    TimeProvider timeProvider) : IRequestHandler<InspectInvitationQuery, Result<InvitationDto>>
{
    public async Task<Result<InvitationDto>> Handle(InspectInvitationQuery query, CancellationToken cancellationToken)
    {
        if (await invitations.GetByTokenHashAsync(InvitationToken.Hash(query.Token), cancellationToken) is not { } invitation)
        {
            return IdentityErrors.InvitationNotFound;
        }

        if (invitation.EnsureUsable(timeProvider.GetUtcNow()).Error is { } unusable)
        {
            return unusable;
        }

        if (await users.GetByIdAsync(invitation.UserId, cancellationToken) is not { HasPendingInvitation: true } user)
        {
            return IdentityErrors.InvitationNotFound;
        }

        return new InvitationDto(user.Name, SensitiveDataMasker.MaskEmail(user.Email), user.Role, invitation.ExpiresAt);
    }
}
