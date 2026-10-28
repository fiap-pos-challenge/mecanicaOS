using MediatR;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.RejectQuote;

public sealed class RejectQuoteCommandHandler(ServiceOrderChanges changes, ICurrentUser currentUser, TimeProvider timeProvider)
    : IRequestHandler<RejectQuoteCommand, Result<ServiceOrderDetailDto>>
{
    public Task<Result<ServiceOrderDetailDto>> Handle(RejectQuoteCommand command, CancellationToken cancellationToken) =>
        changes.ApplyAsync(
            command.ServiceOrderId,
            serviceOrder => serviceOrder.RejectQuote(command.Reason, currentUser.UserId, timeProvider.GetUtcNow()),
            cancellationToken);
}
