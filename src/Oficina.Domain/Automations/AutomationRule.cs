namespace Oficina.Domain.Automations;

public sealed class AutomationRule
{
    private AutomationRule(Guid id, string trigger, string definitionJson, DateTimeOffset createdAt)
    {
        Id = id;
        Trigger = trigger;
        DefinitionJson = definitionJson;
        CreatedAt = createdAt;
        Enabled = true;
    }

    public Guid Id { get; private init; }

    public bool Enabled { get; private set; }

    public string Trigger { get; private set; }

    public string DefinitionJson { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public static AutomationRule Create(string trigger, string definitionJson, DateTimeOffset now) => new(Guid.CreateVersion7(), trigger, definitionJson, now);

    public void Update(string trigger, string definitionJson)
    {
        Trigger = trigger;
        DefinitionJson = definitionJson;
    }

    public void SetEnabled(bool enabled) => Enabled = enabled;
}
