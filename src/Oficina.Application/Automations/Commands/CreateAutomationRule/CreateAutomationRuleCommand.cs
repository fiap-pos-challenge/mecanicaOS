using MediatR;
using Oficina.Application.Automations.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations.Commands.CreateAutomationRule;

public sealed record CreateAutomationRuleCommand(RuleDefinitionDto? Definition) : IRequest<Result<AutomationRuleDto>>;
