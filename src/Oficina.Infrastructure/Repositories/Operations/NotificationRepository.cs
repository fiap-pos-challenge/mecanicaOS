using Microsoft.EntityFrameworkCore;
using Oficina.Application.Notifications;
using Oficina.Domain.Notifications;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Operations;

public sealed class NotificationRepository(OficinaDbContext context) : INotificationRepository, IUserDirectory
{
    public async Task<IReadOnlyList<Notification>> RecentAsync(int count, CancellationToken cancellationToken) =>
        await context.Notifications.AsNoTracking().OrderByDescending(notification => notification.CreatedAt).Take(count).ToListAsync(cancellationToken);

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Notifications.AsNoTracking().SingleOrDefaultAsync(notification => notification.Id == id, cancellationToken);

    public async Task<bool> DedupeExistsAsync(string dedupeKey, CancellationToken cancellationToken) =>
        context.Notifications.Local.Any(notification => notification.DedupeKey == dedupeKey)
        || await context.Notifications.AnyAsync(notification => notification.DedupeKey == dedupeKey, cancellationToken);

    public async Task<IReadOnlyList<Guid>> ReadIdsAsync(Guid userId, IReadOnlyCollection<Guid> notificationIds, CancellationToken cancellationToken) =>
        await context.NotificationReads.AsNoTracking().Where(read => read.UserId == userId && notificationIds.Contains(read.NotificationId)).Select(read => read.NotificationId).ToListAsync(cancellationToken);

    public void Add(Notification notification) => context.Notifications.Add(notification);

    public void Add(NotificationRead read) => context.NotificationReads.Add(read);

    public async Task<IReadOnlyList<(Guid UserId, string Email)>> ActiveEmailsAsync(CancellationToken cancellationToken) =>
        (await context.Users.AsNoTracking().Where(user => user.IsActive).Select(user => new { user.Id, user.Email }).ToListAsync(cancellationToken))
            .Select(user => (user.Id, user.Email.Value)).ToList();
}
