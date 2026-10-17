using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Queries.GetServicePriceHistory;

public sealed class GetServicePriceHistoryQueryHandler(IServiceQueries queries, IPriceHistoryQueries history)
    : IRequestHandler<GetServicePriceHistoryQuery, Result<PriceHistoryDto>>
{
    public async Task<Result<PriceHistoryDto>> Handle(GetServicePriceHistoryQuery query, CancellationToken cancellationToken)
    {
        if (await queries.GetByIdAsync(query.ServiceId, cancellationToken) is null)
        {
            return CatalogErrors.NotFound;
        }

        return new PriceHistoryDto(await history.ListAsync(CatalogItemKind.Service, query.ServiceId, includeCost: false, cancellationToken));
    }
}
