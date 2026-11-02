using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.ServiceOrders.Commands.AddPartItem;

public sealed class AddPartItemCommandHandler(IPartRepository parts, ServiceOrderChanges changes)
    : IRequestHandler<AddPartItemCommand, Result<ServiceOrderDetailDto>>
{
    public async Task<Result<ServiceOrderDetailDto>> Handle(AddPartItemCommand command, CancellationToken cancellationToken)
    {
        var quantity = Quantity.Create(command.Quantity);
        if (quantity.Error is { } invalidQuantity)
        {
            return invalidQuantity;
        }

        if (await parts.GetByIdAsync(command.PartId, cancellationToken) is not { } part)
        {
            return InventoryErrors.NotFound;
        }

        return await changes.ApplyAsync(
            command.ServiceOrderId,
            serviceOrder => serviceOrder.AddPartItem(part, quantity.Value),
            cancellationToken);
    }
}
