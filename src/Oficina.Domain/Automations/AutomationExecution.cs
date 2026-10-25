namespace Oficina.Domain.Automations;

public sealed class AutomationExecution
{
    public const int TextMaxLength = 300;

    private AutomationExecution(Guid id, Guid ruleId, string trigger, Guid? subjectId, string dedupeKey, DateTimeOffset dueAt, DateTimeOffset createdAt)
    {
        Id = id;
        RuleId = ruleId;
        Trigger = trigger;
        SubjectId = subjectId;
        DedupeKey = dedupeKey;
        DueAt = dueAt;
        CreatedAt = createdAt;
        Status = ExecutionStatus.Pending;
        Summary = string.Empty;
    }

    public Guid Id { get; private init; }

    public Guid RuleId { get; private init; }

    public string Trigger { get; private init; }

    public Guid? SubjectId { get; private init; }

    public string DedupeKey { get; private init; }

    public DateTimeOffset DueAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public string Status { get; private set; }

    public int Attempts { get; private set; }

    public string Summary { get; private set; }

    public string? Error { get; private set; }

    public DateTimeOffset? FinishedAt { get; private set; }

    public static AutomationExecution Schedule(Guid ruleId, string trigger, Guid? subjectId, string dedupeKey, DateTimeOffset dueAt, DateTimeOffset now) =>
        new(Guid.CreateVersion7(), ruleId, trigger, subjectId, dedupeKey, dueAt, now);

    public void Finish(string status, string summary, string? error, DateTimeOffset now)
    {
        Status = status;
        Summary = Cut(summary);
        Error = error is null ? null : Cut(error);
        FinishedAt = now;
        Attempts++;
    }

    public void Retry(DateTimeOffset now)
    {
        Status = ExecutionStatus.Pending;
        Error = null;
        DueAt = now;
        FinishedAt = null;
    }

    private static string Cut(string value) => value.Length > TextMaxLength ? value[..TextMaxLength] : value;
}
