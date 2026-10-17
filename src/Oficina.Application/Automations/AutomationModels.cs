namespace Oficina.Application.Automations;

public sealed record RuleConditionDto(string Field, string Operator, string Value);

public sealed record RuleActionDto(string Kind, string? Message);

public sealed record RuleWaitDto(int Amount, string Unit);

public sealed record RuleDefinitionDto(string Name, string Trigger, IReadOnlyList<RuleConditionDto> Conditions, RuleWaitDto? Wait, IReadOnlyList<RuleActionDto> Actions);

public sealed record AutomationRuleDto(Guid Id, bool Enabled, RuleDefinitionDto Definition, int Runs24h, DateTimeOffset? LastRunAt);

public sealed record AutomationRuleListDto(IReadOnlyList<AutomationRuleDto> Items);

public sealed record QueueHealthDto(string Name, int Processed, int Failed, int Pending);

public sealed record QueueHealthListDto(IReadOnlyList<QueueHealthDto> Items);

public sealed record QueueFailureDto(Guid Id, string Queue, string Summary, string Error, int Attempts, DateTimeOffset FailedAt);

public sealed record QueueFailureListDto(IReadOnlyList<QueueFailureDto> Items);

public sealed record RuleStats(Guid RuleId, int Runs24h, DateTimeOffset? LastRunAt);

public sealed record AutomationContext(
    Guid? ServiceOrderId,
    long? Number,
    string? CustomerName,
    string? Vehicle,
    string? Email,
    string? Phone,
    bool ConsentEmail,
    bool ConsentWhatsApp,
    decimal? Amount,
    string? OrderStatus,
    string? QuoteStatus,
    int? DaysOverdue,
    decimal? PartStock,
    Guid? PartId,
    string? PartName,
    string? Link);

public sealed record ScanSubject(string Trigger, Guid SubjectId, string DedupeKey);

public interface IAutomationQueries
{
    Task<AutomationContext?> ContextAsync(string trigger, Guid? subjectId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ScanSubject>> ScanAsync(DateTimeOffset now, CancellationToken cancellationToken);

    Task<IReadOnlyList<RuleStats>> StatsAsync(DateTimeOffset since, CancellationToken cancellationToken);

    Task<IReadOnlyList<QueueHealthDto>> QueuesAsync(DateTimeOffset since, CancellationToken cancellationToken);

    Task<IReadOnlyList<QueueFailureDto>> FailuresAsync(CancellationToken cancellationToken);

    Task<bool> RetryOutboxAsync(Guid id, CancellationToken cancellationToken);
}
