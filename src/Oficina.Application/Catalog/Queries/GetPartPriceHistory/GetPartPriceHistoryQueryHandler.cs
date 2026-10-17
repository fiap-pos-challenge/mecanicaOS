using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Queries.GetPartPriceHistory;

public sealed class GetPartPriceHistoryQueryHandler(IPartQueries queries, IPriceHistoryQueries history)
    : IRequestHandler<GetPartPriceHistoryQuery, Result<PriceHistoryDto>>
{
    public async Task<Result<PriceHistoryDto>> Handle(GetPartPriceHistoryQuery query, CancellationToken cancellationToken)
    {
        if (await queries.GetByIdAsync(query.Id, query.IncludeCost, cancellationToken) is null)
        {
            return InventoryErrors.NotFound;
        }

        return new PriceHistoryDto(await history.ListAsync(CatalogItemKind.Part, query.Id, query.IncludeCost, cancellationToken));
    }
}
