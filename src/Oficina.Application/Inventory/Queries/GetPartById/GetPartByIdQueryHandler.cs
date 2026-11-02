using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Errors;

namespace Oficina.Application.Inventory.Queries.GetPartById;

public sealed class GetPartByIdQueryHandler(IPartQueries queries) : IRequestHandler<GetPartByIdQuery, Result<PartDetailDto>>
{
    public async Task<Result<PartDetailDto>> Handle(GetPartByIdQuery query, CancellationToken cancellationToken)
    {
        if (await queries.GetByIdAsync(query.Id, cancellationToken) is not { } part)
        {
            return InventoryErrors.NotFound;
        }

        return part;
    }
}
