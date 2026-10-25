using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Errors;

namespace Oficina.Application.Inventory.Queries.GetMovements;

public sealed class GetMovementsQueryHandler(IInventoryQueries queries) : IRequestHandler<GetMovementsQuery, Result<StockMovementListDto>>
{
    public async Task<Result<StockMovementListDto>> Handle(GetMovementsQuery request, CancellationToken cancellationToken)
    {
        return await queries.MovementsAsync(request.PartId, cancellationToken) is { } movements ? movements : InventoryErrors.NotFound;
    }
}
