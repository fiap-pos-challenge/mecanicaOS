using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Catalog.Enums;

namespace Oficina.Application.Catalog.Queries;

public interface IPriceHistoryQueries
{
    Task<IReadOnlyList<PriceChangeDto>> ListAsync(CatalogItemKind kind, Guid itemId, bool includeCost, CancellationToken cancellationToken);
}
