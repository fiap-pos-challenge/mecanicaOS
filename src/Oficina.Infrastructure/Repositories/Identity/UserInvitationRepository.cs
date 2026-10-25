using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Identity;

public sealed class UserInvitationRepository(OficinaDbContext context) : IUserInvitationRepository
{
    public Task<UserInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.UserInvitations.SingleOrDefaultAsync(invitation => invitation.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<UserInvitation>> ListOpenAsync(Guid userId, CancellationToken cancellationToken) =>
        await context.UserInvitations
            .Where(invitation => invitation.UserId == userId && invitation.AcceptedAt == null && invitation.RevokedAt == null)
            .ToListAsync(cancellationToken);

    public void Add(UserInvitation invitation) => context.UserInvitations.Add(invitation);
}
