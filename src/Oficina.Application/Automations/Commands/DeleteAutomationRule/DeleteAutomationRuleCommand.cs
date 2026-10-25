using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations.Commands.DeleteAutomationRule;

public sealed record DeleteAutomationRuleCommand(Guid Id) : IRequest<Result>;
