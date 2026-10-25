using MediatR;
using Oficina.Application.Automations.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations.Queries.ListQueueFailures;

public sealed class ListQueueFailuresQueryHandler(IAutomationQueries queries) : IRequestHandler<ListQueueFailuresQuery, Result<QueueFailureListDto>>
{
    public async Task<Result<QueueFailureListDto>> Handle(ListQueueFailuresQuery query, CancellationToken cancellationToken) =>
        new QueueFailureListDto(await queries.FailuresAsync(cancellationToken));
}
