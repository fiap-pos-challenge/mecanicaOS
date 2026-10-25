using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Identity;

public sealed class UserSessionRepository(OficinaDbContext context) : IUserSessionRepository
{
    public Task<UserSession?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.UserSessions.SingleOrDefaultAsync(session => session.TokenHash == tokenHash, cancellationToken);

    public Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.UserSessions.SingleOrDefaultAsync(session => session.Id == id, cancellationToken);

    public void Add(UserSession session) => context.UserSessions.Add(session);
}
