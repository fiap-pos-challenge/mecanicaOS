using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Queries.GetAverageExecutionTime;

public sealed class GetAverageExecutionTimeQueryHandler(IServiceOrderQueries queries)
    : IRequestHandler<GetAverageExecutionTimeQuery, Result<AverageExecutionTimeDto>>
{
    public async Task<Result<AverageExecutionTimeDto>> Handle(GetAverageExecutionTimeQuery query, CancellationToken cancellationToken) =>
        await queries.GetAverageExecutionTimeAsync(query.From, query.To, cancellationToken);
}
