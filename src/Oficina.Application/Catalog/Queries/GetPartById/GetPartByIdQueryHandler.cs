using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Queries.GetPartById;

public sealed class GetPartByIdQueryHandler(IPartQueries queries)
    : IRequestHandler<GetPartByIdQuery, Result<PartDto>>
{
    public async Task<Result<PartDto>> Handle(GetPartByIdQuery query, CancellationToken cancellationToken)
    {
        return await queries.GetByIdAsync(query.Id, query.IncludeCost, cancellationToken) is { } part ? part : InventoryErrors.NotFound;
    }
}
