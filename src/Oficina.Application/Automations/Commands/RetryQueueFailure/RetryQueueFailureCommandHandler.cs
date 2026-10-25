using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Automations.Queries;
using Oficina.Domain.Automations;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations.Commands.RetryQueueFailure;

public sealed class RetryQueueFailureCommandHandler(IAutomationRepository automations, IAutomationQueries queries, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IRequestHandler<RetryQueueFailureCommand, Result>
{
    public async Task<Result> Handle(RetryQueueFailureCommand command, CancellationToken cancellationToken)
    {
        if (await automations.GetExecutionAsync(command.Id, cancellationToken) is { Status: ExecutionStatus.Failed } execution)
        {
            execution.Retry(timeProvider.GetUtcNow());
            return await unitOfWork.CommitAsync(cancellationToken);
        }

        return await queries.RetryOutboxAsync(command.Id, cancellationToken) ? Result.Success() : AutomationErrors.NotFound;
    }
}
