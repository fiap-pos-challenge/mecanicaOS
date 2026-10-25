using Microsoft.EntityFrameworkCore;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Users.Dtos;
using Oficina.Application.Users.Mappers;
using Oficina.Application.Users.Queries;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.Identity;

public sealed class UserQueries(OficinaDbContext context) : IUserQueries
{
    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
        return user is null ? null : (await WithInvitationsAsync([user], cancellationToken))[0];
    }

    public async Task<UserDto?> GetByEmailAsync(Email email, CancellationToken cancellationToken)
    {
        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Email == email, cancellationToken);
        return user is null ? null : (await WithInvitationsAsync([user], cancellationToken))[0];
    }

    public async Task<PagedResult<UserDto>> ListAsync(PageRequest page, UserFilter filter, CancellationToken cancellationToken)
    {
        var users = Filter(context.Users.AsNoTracking(), filter);
        var totalItems = await users.CountAsync(cancellationToken);
        var items = await users
            .OrderBy(user => user.Name)
            .ThenBy(user => user.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<UserDto>(await WithInvitationsAsync(items, cancellationToken), page.Page, page.PageSize, totalItems);
    }

    private static IQueryable<User> Filter(IQueryable<User> users, UserFilter filter)
    {
        if (filter.Role is { } role)
        {
            users = users.Where(user => user.Role == role);
        }

        return filter.Status switch
        {
            UserStatus.Active => users.Where(user => user.IsActive && user.PasswordHash != null),
            UserStatus.Inactive => users.Where(user => !user.IsActive),
            UserStatus.InvitationPending => users.Where(user => user.IsActive && user.PasswordHash == null),
            _ => users,
        };
    }

    private async Task<List<UserDto>> WithInvitationsAsync(IReadOnlyCollection<User> users, CancellationToken cancellationToken)
    {
        var pendingIds = users.Where(user => user.HasPendingInvitation).Select(user => user.Id).ToList();
        var expirations = pendingIds.Count == 0
            ? new Dictionary<Guid, DateTimeOffset>()
            : await context.UserInvitations.AsNoTracking()
                .Where(invitation => pendingIds.Contains(invitation.UserId) && invitation.AcceptedAt == null && invitation.RevokedAt == null)
                .GroupBy(invitation => invitation.UserId)
                .Select(group => new { UserId = group.Key, ExpiresAt = group.Max(invitation => invitation.ExpiresAt) })
                .ToDictionaryAsync(entry => entry.UserId, entry => entry.ExpiresAt, cancellationToken);

        return users.Select(user => user.ToDto(expirations.TryGetValue(user.Id, out var expiresAt) ? expiresAt : null)).ToList();
    }
}
