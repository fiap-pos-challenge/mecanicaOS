using MediatR;
using Oficina.Application.Automations.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations.Queries.ListAutomationRules;

public sealed record ListAutomationRulesQuery : IRequest<Result<AutomationRuleListDto>>;
