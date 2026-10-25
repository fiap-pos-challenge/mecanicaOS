using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Queries.ListPurchaseOrders;

public sealed class ListPurchaseOrdersQueryHandler(IInventoryQueries queries) : IRequestHandler<ListPurchaseOrdersQuery, Result<PurchaseOrderListDto>>
{
    public async Task<Result<PurchaseOrderListDto>> Handle(ListPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        return await queries.OrdersAsync(cancellationToken);
    }
}
