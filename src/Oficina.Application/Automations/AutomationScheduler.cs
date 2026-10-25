using Oficina.Domain.Automations;

namespace Oficina.Application.Automations;

public sealed class AutomationScheduler(IAutomationRepository automations, TimeProvider timeProvider)
{
    public async Task ScheduleAsync(string trigger, Guid? subjectId, string dedupeKey, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        foreach (var rule in await automations.EnabledAsync(trigger, cancellationToken))
        {
            if (await automations.ExecutionExistsAsync(rule.Id, dedupeKey, cancellationToken))
            {
                continue;
            }

            var definition = AutomationDefinitions.Read(rule.DefinitionJson);
            automations.Add(AutomationExecution.Schedule(rule.Id, trigger, subjectId, dedupeKey, now + AutomationDefinitions.Wait(definition.Wait), now));
        }
    }
}
