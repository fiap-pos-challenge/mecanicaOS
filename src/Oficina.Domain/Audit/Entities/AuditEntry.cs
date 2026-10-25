namespace Oficina.Domain.Audit.Entities;

public sealed class AuditEntry
{
    public const int TextMaxLength = 200;

    private AuditEntry(Guid id, DateTimeOffset occurredAt, Guid? actorId, string actorName, string area, string action, string target, string detail)
    {
        Id = id;
        OccurredAt = occurredAt;
        ActorId = actorId;
        ActorName = actorName;
        Area = area;
        Action = action;
        Target = target;
        Detail = detail;
    }

    public Guid Id { get; private init; }

    public DateTimeOffset OccurredAt { get; private init; }

    public Guid? ActorId { get; private init; }

    public string ActorName { get; private init; }

    public string Area { get; private init; }

    public string Action { get; private init; }

    public string Target { get; private init; }

    public string Detail { get; private init; }

    public static AuditEntry Record(DateTimeOffset now, Guid? actorId, string actorName, string area, string action, string target, string detail) =>
        new(Guid.CreateVersion7(), now, actorId, Cut(actorName), area, Cut(action), Cut(target), Cut(detail));

    private static string Cut(string value)
    {
        var text = value.Trim();
        return text.Length > TextMaxLength ? text[..TextMaxLength] : text;
    }
}
