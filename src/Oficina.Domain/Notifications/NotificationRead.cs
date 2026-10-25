namespace Oficina.Domain.Notifications;

public sealed class NotificationRead
{
    private NotificationRead(Guid notificationId, Guid userId, DateTimeOffset readAt)
    {
        NotificationId = notificationId;
        UserId = userId;
        ReadAt = readAt;
    }

    public Guid NotificationId { get; private init; }

    public Guid UserId { get; private init; }

    public DateTimeOffset ReadAt { get; private init; }

    public static NotificationRead Mark(Guid notificationId, Guid userId, DateTimeOffset now) => new(notificationId, userId, now);
}
