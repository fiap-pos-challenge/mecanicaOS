using MediatR;
using Oficina.Application.Automations.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations.Commands.SetAutomationRuleStatus;

public sealed record SetAutomationRuleStatusCommand(Guid Id, bool? Enabled) : IRequest<Result<AutomationRuleDto>>;
