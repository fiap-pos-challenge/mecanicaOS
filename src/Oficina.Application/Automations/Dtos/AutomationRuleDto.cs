namespace Oficina.Application.Automations.Dtos;

public sealed record AutomationRuleDto(Guid Id, bool Enabled, RuleDefinitionDto Definition, int Runs24h, DateTimeOffset? LastRunAt);
