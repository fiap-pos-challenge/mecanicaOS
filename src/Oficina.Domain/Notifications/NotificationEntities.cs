using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Notifications;

public sealed class Notification
{
    public const int TitleMaxLength = 160;
    public const int BodyMaxLength = 500;

    private Notification(Guid id, string type, string title, string body, string? link, bool aiSuggested, string? dedupeKey, DateTimeOffset createdAt)
    {
        Id = id;
        Type = type;
        Title = title;
        Body = body;
        Link = link;
        AiSuggested = aiSuggested;
        DedupeKey = dedupeKey;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private init; }

    public string Type { get; private init; }

    public string Title { get; private init; }

    public string Body { get; private init; }

    public string? Link { get; private init; }

    public bool AiSuggested { get; private init; }

    public string? DedupeKey { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public static Notification Create(string type, string title, string body, string? link, bool aiSuggested, string? dedupeKey, DateTimeOffset now) =>
        new(Guid.CreateVersion7(), type, Cut(title, TitleMaxLength), Cut(body, BodyMaxLength), link, aiSuggested, dedupeKey, now);

    private static string Cut(string value, int max) => value.Length > max ? value[..max] : value;
}

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

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> RecentAsync(int count, CancellationToken cancellationToken);

    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> DedupeExistsAsync(string dedupeKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ReadIdsAsync(Guid userId, IReadOnlyCollection<Guid> notificationIds, CancellationToken cancellationToken);

    void Add(Notification notification);

    void Add(NotificationRead read);
}

public static class NotificationErrors
{
    public static readonly Error NotFound = new("notifications.notFound", "Notificação não encontrada.", ErrorType.NotFound);

    public static readonly Error InvalidPreferences = new("notifications.preferences", "Preferências inválidas.", ErrorType.Validation,
        [new FieldError("items", "notifications.preferences", "Preferências inválidas.")]);
}
