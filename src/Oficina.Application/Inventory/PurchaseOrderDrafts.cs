using Oficina.Application.Dashboard;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Dashboard;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Inventory;

public sealed class PurchaseOrderDrafts(IPartRepository parts, ISupplierQueries suppliers, IPurchaseOrderRepository purchaseOrders)
{
    public async Task<Result<PurchaseOrder>> CreateAsync(Guid partId, CancellationToken cancellationToken)
    {
        if (await parts.GetByIdAsync(partId, cancellationToken) is not { } part || await suppliers.PreferredAsync(part.Supplier, cancellationToken) is not { } supplierId)
        {
            return DashboardErrors.NoSupplier;
        }

        var quantity = Math.Max(1, Math.Ceiling((part.MinimumStock * 2) - part.StockQuantity));
        var order = PurchaseOrder.Create(await purchaseOrders.NextNumberAsync(cancellationToken), supplierId, [new PurchaseOrderLine(part.Id, quantity, part.Cost.Amount)]);
        if (order.IsSuccess)
        {
            purchaseOrders.Add(order.Value);
        }

        return order;
    }
}
