namespace Oficina.Domain.Automations;

public interface IAutomationRepository
{
    Task<IReadOnlyList<AutomationRule>> RulesAsync(CancellationToken cancellationToken);

    Task<AutomationRule?> GetRuleAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AutomationRule>> EnabledAsync(string trigger, CancellationToken cancellationToken);

    Task<bool> ExecutionExistsAsync(Guid ruleId, string dedupeKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<AutomationExecution>> DueAsync(DateTimeOffset now, int count, CancellationToken cancellationToken);

    Task<AutomationExecution?> GetExecutionAsync(Guid id, CancellationToken cancellationToken);

    void Add(AutomationRule rule);

    void Add(AutomationExecution execution);

    void Remove(AutomationRule rule);
}
