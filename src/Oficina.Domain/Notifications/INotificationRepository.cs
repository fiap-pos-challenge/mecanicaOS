namespace Oficina.Domain.Notifications;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> RecentAsync(int count, CancellationToken cancellationToken);

    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> DedupeExistsAsync(string dedupeKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ReadIdsAsync(Guid userId, IReadOnlyCollection<Guid> notificationIds, CancellationToken cancellationToken);

    void Add(Notification notification);

    void Add(NotificationRead read);
}
