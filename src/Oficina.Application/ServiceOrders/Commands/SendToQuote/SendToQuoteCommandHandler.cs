using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Application.ServiceOrders.Commands.SendToQuote;

public sealed class SendToQuoteCommandHandler(
    IServiceOrderRepository serviceOrders,
    IServiceRepository services,
    IPartRepository parts,
    IServiceOrderBoardQueries queries,
    IUnitOfWork unitOfWork) : IRequestHandler<SendToQuoteCommand, Result<MechanicOrderDto>>
{
    public async Task<Result<MechanicOrderDto>> Handle(SendToQuoteCommand command, CancellationToken cancellationToken)
    {
        if (await serviceOrders.GetByIdAsync(command.Id, cancellationToken) is not { } order)
        {
            return ServiceOrderErrors.NotFound;
        }

        var marked = order.MarkSentToQuote(command.Description);
        if (marked.Error is { } invalid)
        {
            return invalid;
        }

        var ids = command.Items.Select(item => item.ItemId).Distinct().ToList();
        foreach (var service in await services.ListByIdsAsync(ids, cancellationToken))
        {
            if (!order.HasItem(service.Id) && order.AddServiceItem(service, 1).Error is { } serviceError)
            {
                return serviceError;
            }
        }

        foreach (var part in await parts.ListByIdsAsync(ids, cancellationToken))
        {
            if (!order.HasItem(part.Id) && order.AddPartItem(part, Quantity.Create(1m).Value).Error is { } partError)
            {
                return partError;
            }
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return await ServiceOrderResponses.WorkAsync(queries, order.Id, cancellationToken);
    }
}
