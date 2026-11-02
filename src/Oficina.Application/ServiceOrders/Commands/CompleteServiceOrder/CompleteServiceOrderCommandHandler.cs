using MediatR;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.CompleteServiceOrder;

public sealed class CompleteServiceOrderCommandHandler(ServiceOrderChanges changes, ICurrentUser currentUser, TimeProvider timeProvider)
    : IRequestHandler<CompleteServiceOrderCommand, Result<ServiceOrderDetailDto>>
{
    public Task<Result<ServiceOrderDetailDto>> Handle(CompleteServiceOrderCommand command, CancellationToken cancellationToken) =>
        changes.ApplyAsync(
            command.ServiceOrderId,
            serviceOrder => serviceOrder.Complete(currentUser.UserId, timeProvider.GetUtcNow()),
            cancellationToken);
}
