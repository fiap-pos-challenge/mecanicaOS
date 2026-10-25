using Oficina.Domain.Common.Events;

namespace Oficina.Domain.Notifications;

public sealed class Notification : IHasDomainEvents
{
    public const int TitleMaxLength = 160;
    public const int BodyMaxLength = 500;

    private readonly List<IDomainEvent> _domainEvents;

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
        _domainEvents = new List<IDomainEvent>();
    }

    public Guid Id { get; private init; }

    public string Type { get; private init; }

    public string Title { get; private init; }

    public string Body { get; private init; }

    public string? Link { get; private init; }

    public bool AiSuggested { get; private init; }

    public string? DedupeKey { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Notification Create(string type, string title, string body, string? link, bool aiSuggested, string? dedupeKey, DateTimeOffset now)
    {
        var notification = new Notification(Guid.CreateVersion7(), type, Cut(title, TitleMaxLength), Cut(body, BodyMaxLength), link, aiSuggested, dedupeKey, now);
        notification._domainEvents.Add(new NotificationCreated(notification.Id, notification.Type, notification.Title, notification.Body, notification.Link, notification.AiSuggested, now));
        return notification;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private static string Cut(string value, int max) => value.Length > max ? value[..max] : value;
}
