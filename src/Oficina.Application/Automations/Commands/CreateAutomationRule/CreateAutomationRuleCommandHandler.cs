using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Automations.Dtos;
using Oficina.Application.Automations.Queries;
using Oficina.Domain.Automations;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations.Commands.CreateAutomationRule;

public sealed class CreateAutomationRuleCommandHandler(IAutomationRepository automations, IAutomationQueries queries, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IRequestHandler<CreateAutomationRuleCommand, Result<AutomationRuleDto>>
{
    public async Task<Result<AutomationRuleDto>> Handle(CreateAutomationRuleCommand command, CancellationToken cancellationToken)
    {
        if (AutomationDefinitions.Check(command.Definition) is { } error)
        {
            return error;
        }

        var now = timeProvider.GetUtcNow();
        var rule = AutomationRule.Create(command.Definition!.Trigger, AutomationDefinitions.Write(command.Definition), now);
        automations.Add(rule);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return rule.ToDto(await queries.StatsAsync(now.AddDays(-1), cancellationToken));
    }
}
