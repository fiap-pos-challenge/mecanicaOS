using Oficina.Application.Automations.Dtos;

namespace Oficina.Application.Automations.Queries;

public interface IAutomationQueries
{
    Task<AutomationContext?> ContextAsync(string trigger, Guid? subjectId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ScanSubject>> ScanAsync(DateTimeOffset now, CancellationToken cancellationToken);

    Task<IReadOnlyList<RuleStats>> StatsAsync(DateTimeOffset since, CancellationToken cancellationToken);

    Task<IReadOnlyList<QueueHealthDto>> QueuesAsync(DateTimeOffset since, CancellationToken cancellationToken);

    Task<IReadOnlyList<QueueFailureDto>> FailuresAsync(CancellationToken cancellationToken);

    Task<bool> RetryOutboxAsync(Guid id, CancellationToken cancellationToken);
}
