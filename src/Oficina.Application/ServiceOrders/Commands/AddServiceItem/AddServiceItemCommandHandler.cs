using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.AddServiceItem;

public sealed class AddServiceItemCommandHandler(IServiceRepository services, ServiceOrderChanges changes)
    : IRequestHandler<AddServiceItemCommand, Result<ServiceOrderDetailDto>>
{
    public async Task<Result<ServiceOrderDetailDto>> Handle(AddServiceItemCommand command, CancellationToken cancellationToken)
    {
        if (await services.GetByIdAsync(command.ServiceId, cancellationToken) is not { } service)
        {
            return CatalogErrors.NotFound;
        }

        return await changes.ApplyAsync(
            command.ServiceOrderId,
            serviceOrder => serviceOrder.AddServiceItem(service, command.Quantity),
            cancellationToken);
    }
}
