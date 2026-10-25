namespace Oficina.Domain.Dashboard;

public sealed class DashboardAction
{
    private DashboardAction(Guid id, string itemId, string action, Guid? userId, DateTimeOffset occurredAt)
    {
        Id = id;
        ItemId = itemId;
        Action = action;
        UserId = userId;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private init; }

    public string ItemId { get; private init; }

    public string Action { get; private init; }

    public Guid? UserId { get; private init; }

    public DateTimeOffset OccurredAt { get; private init; }

    public static DashboardAction Record(string itemId, string action, Guid? userId, DateTimeOffset now) => new(Guid.CreateVersion7(), itemId, action, userId, now);
}
