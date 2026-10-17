using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Application.ServiceOrders.Commands.StartService;

public sealed class StartServiceCommandHandler(
    IServiceOrderRepository serviceOrders,
    IServiceOrderBoardQueries queries,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<StartServiceCommand, Result<MechanicOrderDto>>
{
    public async Task<Result<MechanicOrderDto>> Handle(StartServiceCommand command, CancellationToken cancellationToken)
    {
        if (await serviceOrders.GetByIdAsync(command.Id, cancellationToken) is not { } order)
        {
            return ServiceOrderErrors.NotFound;
        }

        var now = timeProvider.GetUtcNow();
        if (currentUser.UserId is not { } userId)
        {
            return CommonErrors.Unauthorized;
        }

        order.AssignMechanic(userId);
        var result = order.Status == ServiceOrderStatus.Received ? order.StartDiagnosis(userId, now) : order.StartTimer(userId, now);
        if (result.Error is { } invalid)
        {
            return invalid;
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return await ServiceOrderResponses.WorkAsync(queries, order.Id, cancellationToken);
    }
}
