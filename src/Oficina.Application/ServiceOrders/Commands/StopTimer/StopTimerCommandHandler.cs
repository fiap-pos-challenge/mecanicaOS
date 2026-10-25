using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Application.ServiceOrders.Commands.StopTimer;

public sealed class StopTimerCommandHandler(
    IServiceOrderRepository serviceOrders,
    IServiceOrderQueries queries,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<StopTimerCommand, Result<ServiceOrderDetailDto>>
{
    public async Task<Result<ServiceOrderDetailDto>> Handle(StopTimerCommand command, CancellationToken cancellationToken)
    {
        if (await serviceOrders.GetByIdAsync(command.Id, cancellationToken) is not { } order)
        {
            return ServiceOrderErrors.NotFound;
        }

        var now = timeProvider.GetUtcNow();
        order.StopTimer(now);
        var result = Result.Success();
        if (result.Error is { } invalid)
        {
            return invalid;
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return await ServiceOrderResponses.DetailAsync(queries, order.Id, cancellationToken);
    }
}
