using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Queries.GetStockOverview;

public sealed class GetStockOverviewQueryHandler(IInventoryQueries queries) : IRequestHandler<GetStockOverviewQuery, Result<StockOverviewDto>>
{
    public async Task<Result<StockOverviewDto>> Handle(GetStockOverviewQuery request, CancellationToken cancellationToken)
    {
        return await queries.OverviewAsync(new PageRequest(request.Page, request.PageSize), request.Term, request.Situation, request.Location, cancellationToken);
    }
}
