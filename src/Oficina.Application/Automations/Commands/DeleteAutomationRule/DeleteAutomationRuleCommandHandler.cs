using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Domain.Automations;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations.Commands.DeleteAutomationRule;

public sealed class DeleteAutomationRuleCommandHandler(IAutomationRepository automations, IUnitOfWork unitOfWork) : IRequestHandler<DeleteAutomationRuleCommand, Result>
{
    public async Task<Result> Handle(DeleteAutomationRuleCommand command, CancellationToken cancellationToken)
    {
        if (await automations.GetRuleAsync(command.Id, cancellationToken) is not { } rule)
        {
            return AutomationErrors.NotFound;
        }

        automations.Remove(rule);
        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
