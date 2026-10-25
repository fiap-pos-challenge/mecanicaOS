using Oficina.Application.Automations.Dtos;

namespace Oficina.Api.Contracts.Operations;

public sealed record AutomationRuleBody(RuleDefinitionDto? Definition);
