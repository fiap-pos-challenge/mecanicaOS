using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Invitations;
using Oficina.Application.Quotes.Decisions;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;
using Oficina.Domain.ServiceOrders.ValueObjects;

namespace Oficina.Application.ServiceOrders.Commands.TransitionServiceOrder;

public sealed class TransitionServiceOrderCommandHandler(
    IServiceOrderRepository serviceOrders,
    QuoteDecider decider,
    IServiceOrderQueries queries,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<TransitionServiceOrderCommand, Result<ServiceOrderDetailDto>>
{
    public const int MinRejectionReasonLength = 5;

    public async Task<Result<ServiceOrderDetailDto>> Handle(TransitionServiceOrderCommand command, CancellationToken cancellationToken)
    {
        if (await serviceOrders.GetByIdAsync(command.Id, cancellationToken) is not { } order)
        {
            return ServiceOrderErrors.NotFound;
        }

        if (order.Version != command.Version)
        {
            return ServiceOrderErrors.VersionConflict;
        }

        var moved = await MoveAsync(order, command, timeProvider.GetUtcNow(), cancellationToken);
        if (moved.Error is { } transitionError)
        {
            return transitionError;
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return await ServiceOrderResponses.DetailAsync(queries, order.Id, cancellationToken);
    }

    private async Task<Result> MoveAsync(ServiceOrder order, TransitionServiceOrderCommand command, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        return (order.Status, command.To) switch
        {
            (ServiceOrderStatus.Received, ServiceOrderStatus.InDiagnosis) => order.StartDiagnosis(userId, now),
            (ServiceOrderStatus.InDiagnosis, ServiceOrderStatus.WaitingApproval) =>
                order.SendQuote(ApprovalToken.FromRawToken(InvitationToken.Generate()).Value, userId, now),
            (ServiceOrderStatus.WaitingApproval, ServiceOrderStatus.InProgress) => await decider.ApproveServiceOrderAsync(order, userId, cancellationToken),
            (ServiceOrderStatus.WaitingApproval, ServiceOrderStatus.InDiagnosis) =>
                (command.Reason?.Trim().Length ?? 0) < MinRejectionReasonLength
                    ? ServiceOrderErrors.RejectionReasonRequired
                    : await decider.RejectServiceOrderAsync(order, command.Reason!, cancellationToken),
            (ServiceOrderStatus.InProgress, ServiceOrderStatus.Completed) => order.Complete(userId, now),
            (ServiceOrderStatus.Completed, ServiceOrderStatus.Delivered) => order.Deliver(userId, now),
            _ => ServiceOrderErrors.InvalidTransition,
        };
    }
}
