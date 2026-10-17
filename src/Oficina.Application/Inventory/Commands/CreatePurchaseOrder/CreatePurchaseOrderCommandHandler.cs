using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Inventory.Dtos;
using Oficina.Application.Inventory.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Inventory.Commands.CreatePurchaseOrder;

public sealed class CreatePurchaseOrderCommandHandler(IPurchaseOrderRepository orders, ISupplierRepository suppliers, IPartRepository parts, IInventoryQueries queries, IUnitOfWork unitOfWork) : IRequestHandler<CreatePurchaseOrderCommand, Result<PurchaseOrderDto>>
{
    public async Task<Result<PurchaseOrderDto>> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var stock = await parts.ListByIdsAsync(request.Lines.Select(line => line.PartId).Distinct().ToList(), cancellationToken);
        var valid = request.Lines.All(line => stock.FirstOrDefault(part => part.Id == line.PartId) is { } part
            && Quantity.Create(line.Quantity) is { IsSuccess: true } quantity && part.EnsureQuantityUnit(quantity.Value).IsSuccess);
        if (await suppliers.GetByIdAsync(request.SupplierId, cancellationToken) is null || !valid)
        {
            return InventoryErrors.InvalidOrder;
        }

        var created = PurchaseOrder.Create(await orders.NextNumberAsync(cancellationToken), request.SupplierId, request.Lines.Select(line => new PurchaseOrderLine(line.PartId, line.Quantity, line.UnitCost)).ToList());
        if (created.Error is { } invalid)
        {
            return invalid;
        }

        orders.Add(created.Value);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return (await queries.OrderAsync(created.Value.Id, cancellationToken))!;
    }
}
