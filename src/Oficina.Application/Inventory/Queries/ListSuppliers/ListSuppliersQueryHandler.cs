using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Queries.ListSuppliers;

public sealed class ListSuppliersQueryHandler(IInventoryQueries queries) : IRequestHandler<ListSuppliersQuery, Result<SupplierListDto>>
{
    public async Task<Result<SupplierListDto>> Handle(ListSuppliersQuery request, CancellationToken cancellationToken)
    {
        return await queries.SuppliersAsync(cancellationToken);
    }
}
