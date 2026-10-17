using Microsoft.EntityFrameworkCore;
using Oficina.Application.Notifications;
using Oficina.Domain.Automations;
using Oficina.Domain.Notifications;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Operations;

public sealed class NotificationRepository(OficinaDbContext context) : INotificationRepository, IUserDirectory
{
    public async Task<IReadOnlyList<Notification>> RecentAsync(int count, CancellationToken cancellationToken) =>
        await context.Notifications.AsNoTracking().OrderByDescending(notification => notification.CreatedAt).Take(count).ToListAsync(cancellationToken);

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Notifications.AsNoTracking().SingleOrDefaultAsync(notification => notification.Id == id, cancellationToken);

    public async Task<bool> DedupeExistsAsync(string dedupeKey, CancellationToken cancellationToken) =>
        context.Notifications.Local.Any(notification => notification.DedupeKey == dedupeKey)
        || await context.Notifications.AnyAsync(notification => notification.DedupeKey == dedupeKey, cancellationToken);

    public async Task<IReadOnlyList<Guid>> ReadIdsAsync(Guid userId, IReadOnlyCollection<Guid> notificationIds, CancellationToken cancellationToken) =>
        await context.NotificationReads.AsNoTracking().Where(read => read.UserId == userId && notificationIds.Contains(read.NotificationId)).Select(read => read.NotificationId).ToListAsync(cancellationToken);

    public void Add(Notification notification) => context.Notifications.Add(notification);

    public void Add(NotificationRead read) => context.NotificationReads.Add(read);

    public async Task<IReadOnlyList<(Guid UserId, string Email)>> ActiveEmailsAsync(CancellationToken cancellationToken) =>
        (await context.Users.AsNoTracking().Where(user => user.IsActive).Select(user => new { user.Id, user.Email }).ToListAsync(cancellationToken))
            .Select(user => (user.Id, user.Email.Value)).ToList();
}

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

    public void Remove(AutomationRule rule) => context.AutomationRules.Remove(rule);

    public void Add(AutomationExecution execution) => context.AutomationExecutions.Add(execution);
}
