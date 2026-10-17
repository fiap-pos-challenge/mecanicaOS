using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Queries.GetPackagePriceHistory;

public sealed class GetPackagePriceHistoryQueryHandler(IPackageQueries queries, IPriceHistoryQueries history)
    : IRequestHandler<GetPackagePriceHistoryQuery, Result<PriceHistoryDto>>
{
    public async Task<Result<PriceHistoryDto>> Handle(GetPackagePriceHistoryQuery query, CancellationToken cancellationToken)
    {
        if (await queries.GetByIdAsync(query.Id, cancellationToken) is null)
        {
            return PackageErrors.NotFound;
        }

        return new PriceHistoryDto(await history.ListAsync(CatalogItemKind.Package, query.Id, includeCost: false, cancellationToken));
    }
}
