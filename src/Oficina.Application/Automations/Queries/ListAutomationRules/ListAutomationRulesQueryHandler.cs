using MediatR;
using Oficina.Application.Automations.Dtos;
using Oficina.Domain.Automations;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations.Queries.ListAutomationRules;

public sealed class ListAutomationRulesQueryHandler(IAutomationRepository automations, IAutomationQueries queries, TimeProvider timeProvider)
    : IRequestHandler<ListAutomationRulesQuery, Result<AutomationRuleListDto>>
{
    public async Task<Result<AutomationRuleListDto>> Handle(ListAutomationRulesQuery query, CancellationToken cancellationToken)
    {
        var stats = await queries.StatsAsync(timeProvider.GetUtcNow().AddDays(-1), cancellationToken);
        return new AutomationRuleListDto((await automations.RulesAsync(cancellationToken)).Select(rule => rule.ToDto(stats)).ToList());
    }
}
