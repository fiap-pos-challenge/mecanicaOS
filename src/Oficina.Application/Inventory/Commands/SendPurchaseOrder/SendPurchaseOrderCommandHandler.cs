using MediatR;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Inventory.Dtos;
using Oficina.Application.Inventory.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Inventory.Commands.SendPurchaseOrder;

public sealed class SendPurchaseOrderCommandHandler(IPurchaseOrderRepository orders, ISupplierRepository suppliers, IInventoryQueries queries, IEmailSender emailSender, IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<SendPurchaseOrderCommand, Result<PurchaseOrderDto>>
{
    public async Task<Result<PurchaseOrderDto>> Handle(SendPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        if (await orders.GetByIdAsync(request.Id, cancellationToken) is not { } order)
        {
            return InventoryErrors.OrderNotFound;
        }

        if (order.Queue(timeProvider.GetUtcNow()).Error is { } invalid)
        {
            return invalid;
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        var detail = (await queries.OrderAsync(order.Id, cancellationToken))!;
        var supplier = await suppliers.GetByIdAsync(order.SupplierId, cancellationToken);
        if (supplier?.Email is { } email && await emailSender.SendAsync(PurchaseOrderEmail.Message(email, detail), cancellationToken))
        {
            order.MarkSent();
            await unitOfWork.CommitAsync(cancellationToken);
            detail = (await queries.OrderAsync(order.Id, cancellationToken))!;
        }

        return detail;
    }
}
