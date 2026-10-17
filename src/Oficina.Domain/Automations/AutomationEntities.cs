using Oficina.Domain.Common.Results;

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

public static class ExecutionStatus
{
    public const string Pending = "Pending";
    public const string Done = "Done";
    public const string Skipped = "Skipped";
    public const string Failed = "Failed";
}

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

public interface IAutomationRepository
{
    Task<IReadOnlyList<AutomationRule>> RulesAsync(CancellationToken cancellationToken);

    Task<AutomationRule?> GetRuleAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AutomationRule>> EnabledAsync(string trigger, CancellationToken cancellationToken);

    Task<bool> ExecutionExistsAsync(Guid ruleId, string dedupeKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<AutomationExecution>> DueAsync(DateTimeOffset now, int count, CancellationToken cancellationToken);

    Task<AutomationExecution?> GetExecutionAsync(Guid id, CancellationToken cancellationToken);

    void Add(AutomationRule rule);

    void Remove(AutomationRule rule);

    void Add(AutomationExecution execution);
}

public static class AutomationErrors
{
    public static readonly Error NotFound = new("automations.notFound", "Automação não encontrada.", ErrorType.NotFound);

    public static readonly Error InvalidDefinition = new("automations.definition", "Regra inválida.", ErrorType.Validation,
        [new FieldError("definition", "automations.definition", "Regra inválida.")]);

    public static readonly Error InvalidEnabled = new("automations.enabled", "Informe se a regra fica ligada.", ErrorType.Validation,
        [new FieldError("enabled", "automations.enabled", "Informe se a regra fica ligada.")]);

    public static Error Invalid(string message) => new("automations.invalid", message, ErrorType.BusinessRule);
}
