using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Identity;

public sealed class UserRepository(OficinaDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken) =>
        context.Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken) =>
        context.Users.CountAsync(user => user.IsActive && user.Role == Role.Admin, cancellationToken);

    public void Add(User user) => context.Users.Add(user);

    public void Remove(User user) => context.Users.Remove(user);
}
