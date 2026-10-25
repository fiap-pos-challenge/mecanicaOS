namespace Oficina.Application.Automations.Dtos;

public sealed record RuleDefinitionDto(string Name, string Trigger, IReadOnlyList<RuleConditionDto> Conditions, RuleWaitDto? Wait, IReadOnlyList<RuleActionDto> Actions);
