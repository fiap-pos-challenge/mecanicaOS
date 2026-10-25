using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Errors;

namespace Oficina.Application.Inventory.Queries.GetCount;

public sealed class GetCountQueryHandler(IInventoryQueries queries) : IRequestHandler<GetCountQuery, Result<StockCountDto>>
{
    public async Task<Result<StockCountDto>> Handle(GetCountQuery request, CancellationToken cancellationToken)
    {
        return await queries.CountAsync(request.Id, cancellationToken) is { } count ? count : InventoryErrors.CountNotFound;
    }
}
