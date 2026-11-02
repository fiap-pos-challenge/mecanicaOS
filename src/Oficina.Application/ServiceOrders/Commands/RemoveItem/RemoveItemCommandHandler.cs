using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.RemoveItem;

public sealed class RemoveItemCommandHandler(ServiceOrderChanges changes)
    : IRequestHandler<RemoveItemCommand, Result<ServiceOrderDetailDto>>
{
    public Task<Result<ServiceOrderDetailDto>> Handle(RemoveItemCommand command, CancellationToken cancellationToken) =>
        changes.ApplyAsync(command.ServiceOrderId, serviceOrder => serviceOrder.RemoveItem(command.ItemId), cancellationToken);
}
