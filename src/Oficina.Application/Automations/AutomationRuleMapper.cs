using Oficina.Application.Automations.Dtos;
using Oficina.Application.Automations.Queries;
using Oficina.Domain.Automations;

namespace Oficina.Application.Automations;

public static class AutomationRuleMapper
{
    public static AutomationRuleDto ToDto(this AutomationRule rule, IReadOnlyList<RuleStats> stats)
    {
        var stat = stats.FirstOrDefault(item => item.RuleId == rule.Id);
        return new AutomationRuleDto(rule.Id, rule.Enabled, AutomationDefinitions.Read(rule.DefinitionJson), stat?.Runs24h ?? 0, stat?.LastRunAt);
    }
}
