using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Queries.ListCounts;

public sealed class ListCountsQueryHandler(IInventoryQueries queries) : IRequestHandler<ListCountsQuery, Result<StockCountListDto>>
{
    public async Task<Result<StockCountListDto>> Handle(ListCountsQuery request, CancellationToken cancellationToken)
    {
        return await queries.CountsAsync(cancellationToken);
    }
}
