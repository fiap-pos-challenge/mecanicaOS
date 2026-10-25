namespace Oficina.Domain.Reports;

public sealed class SavedReport
{
    public const int NameMaxLength = 80;

    private SavedReport(Guid id, string name, string definitionJson, DateTimeOffset updatedAt)
    {
        Id = id;
        Name = name;
        DefinitionJson = definitionJson;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private init; }

    public string Name { get; private set; }

    public string DefinitionJson { get; private set; }

    public string? ScheduleJson { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? LastSentAt { get; private set; }

    public static SavedReport Create(string name, string definitionJson, DateTimeOffset now) => new(Guid.CreateVersion7(), name, definitionJson, now);

    public void Update(string name, string definitionJson, DateTimeOffset now)
    {
        Name = name;
        DefinitionJson = definitionJson;
        UpdatedAt = now;
    }

    public void Schedule(string? scheduleJson, DateTimeOffset now)
    {
        ScheduleJson = scheduleJson;
        UpdatedAt = now;
        LastSentAt = null;
    }

    public void MarkSent(DateTimeOffset now) => LastSentAt = now;
}
