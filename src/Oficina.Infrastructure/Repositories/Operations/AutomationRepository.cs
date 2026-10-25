using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Automations;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Operations;

public sealed class AutomationRepository(OficinaDbContext context) : IAutomationRepository
{
    public async Task<IReadOnlyList<AutomationRule>> RulesAsync(CancellationToken cancellationToken) =>
        await context.AutomationRules.OrderByDescending(rule => rule.CreatedAt).ToListAsync(cancellationToken);

    public Task<AutomationRule?> GetRuleAsync(Guid id, CancellationToken cancellationToken) =>
        context.AutomationRules.SingleOrDefaultAsync(rule => rule.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AutomationRule>> EnabledAsync(string trigger, CancellationToken cancellationToken) =>
        await context.AutomationRules.Where(rule => rule.Enabled && rule.Trigger == trigger).ToListAsync(cancellationToken);

    public async Task<bool> ExecutionExistsAsync(Guid ruleId, string dedupeKey, CancellationToken cancellationToken) =>
        context.AutomationExecutions.Local.Any(execution => execution.RuleId == ruleId && execution.DedupeKey == dedupeKey)
        || await context.AutomationExecutions.AnyAsync(execution => execution.RuleId == ruleId && execution.DedupeKey == dedupeKey, cancellationToken);

    public async Task<IReadOnlyList<AutomationExecution>> DueAsync(DateTimeOffset now, int count, CancellationToken cancellationToken) =>
        await context.AutomationExecutions.Where(execution => execution.Status == ExecutionStatus.Pending && execution.DueAt <= now).OrderBy(execution => execution.DueAt).Take(count).ToListAsync(cancellationToken);

    public Task<AutomationExecution?> GetExecutionAsync(Guid id, CancellationToken cancellationToken) =>
        context.AutomationExecutions.SingleOrDefaultAsync(execution => execution.Id == id, cancellationToken);

    public void Add(AutomationRule rule) => context.AutomationRules.Add(rule);

    public void Add(AutomationExecution execution) => context.AutomationExecutions.Add(execution);

    public void Remove(AutomationRule rule) => context.AutomationRules.Remove(rule);
}
