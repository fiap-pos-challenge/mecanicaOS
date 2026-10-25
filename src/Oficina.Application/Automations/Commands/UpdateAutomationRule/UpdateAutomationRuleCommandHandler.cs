using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Automations.Dtos;
using Oficina.Application.Automations.Queries;
using Oficina.Domain.Automations;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations.Commands.UpdateAutomationRule;

public sealed class UpdateAutomationRuleCommandHandler(IAutomationRepository automations, IAutomationQueries queries, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IRequestHandler<UpdateAutomationRuleCommand, Result<AutomationRuleDto>>
{
    public async Task<Result<AutomationRuleDto>> Handle(UpdateAutomationRuleCommand command, CancellationToken cancellationToken)
    {
        if (await automations.GetRuleAsync(command.Id, cancellationToken) is not { } rule)
        {
            return AutomationErrors.NotFound;
        }

        if (AutomationDefinitions.Check(command.Definition) is { } error)
        {
            return error;
        }

        rule.Update(command.Definition!.Trigger, AutomationDefinitions.Write(command.Definition));
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return rule.ToDto(await queries.StatsAsync(timeProvider.GetUtcNow().AddDays(-1), cancellationToken));
    }
}
