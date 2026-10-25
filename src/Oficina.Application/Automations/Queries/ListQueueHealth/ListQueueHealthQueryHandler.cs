using MediatR;
using Oficina.Application.Automations.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations.Queries.ListQueueHealth;

public sealed class ListQueueHealthQueryHandler(IAutomationQueries queries, TimeProvider timeProvider) : IRequestHandler<ListQueueHealthQuery, Result<QueueHealthListDto>>
{
    public async Task<Result<QueueHealthListDto>> Handle(ListQueueHealthQuery query, CancellationToken cancellationToken) =>
        new QueueHealthListDto(await queries.QueuesAsync(timeProvider.GetUtcNow().AddDays(-1), cancellationToken));
}
