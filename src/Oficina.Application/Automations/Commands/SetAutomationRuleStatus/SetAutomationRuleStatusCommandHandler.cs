using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Automations.Dtos;
using Oficina.Application.Automations.Queries;
using Oficina.Domain.Automations;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations.Commands.SetAutomationRuleStatus;

public sealed class SetAutomationRuleStatusCommandHandler(IAutomationRepository automations, IAutomationQueries queries, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IRequestHandler<SetAutomationRuleStatusCommand, Result<AutomationRuleDto>>
{
    public async Task<Result<AutomationRuleDto>> Handle(SetAutomationRuleStatusCommand command, CancellationToken cancellationToken)
    {
        if (await automations.GetRuleAsync(command.Id, cancellationToken) is not { } rule)
        {
            return AutomationErrors.NotFound;
        }

        if (command.Enabled is not { } enabled)
        {
            return AutomationErrors.InvalidEnabled;
        }

        rule.SetEnabled(enabled);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return rule.ToDto(await queries.StatsAsync(timeProvider.GetUtcNow().AddDays(-1), cancellationToken));
    }
}
