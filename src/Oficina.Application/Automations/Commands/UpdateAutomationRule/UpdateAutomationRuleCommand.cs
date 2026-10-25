using MediatR;
using Oficina.Application.Automations.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations.Commands.UpdateAutomationRule;

public sealed record UpdateAutomationRuleCommand(Guid Id, RuleDefinitionDto? Definition) : IRequest<Result<AutomationRuleDto>>;
