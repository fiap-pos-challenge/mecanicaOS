using MediatR;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.DeliverServiceOrder;

public sealed class DeliverServiceOrderCommandHandler(ServiceOrderChanges changes, ICurrentUser currentUser, TimeProvider timeProvider)
    : IRequestHandler<DeliverServiceOrderCommand, Result<ServiceOrderDetailDto>>
{
    public Task<Result<ServiceOrderDetailDto>> Handle(DeliverServiceOrderCommand command, CancellationToken cancellationToken) =>
        changes.ApplyAsync(
            command.ServiceOrderId,
            serviceOrder => serviceOrder.Deliver(currentUser.UserId, timeProvider.GetUtcNow()),
            cancellationToken);
}
