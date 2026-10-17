using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Application.ServiceOrders.Commands.UpdateChecklist;

public sealed class UpdateChecklistCommandHandler(
    IServiceOrderRepository serviceOrders,
    IServiceOrderBoardQueries queries,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<UpdateChecklistCommand, Result<MechanicOrderDto>>
{
    public async Task<Result<MechanicOrderDto>> Handle(UpdateChecklistCommand command, CancellationToken cancellationToken)
    {
        if (await serviceOrders.GetByIdAsync(command.Id, cancellationToken) is not { } order)
        {
            return ServiceOrderErrors.NotFound;
        }

        var now = timeProvider.GetUtcNow();
        var result = order.UpdateChecklist(command.Items.Select(item => (item.Id, item.State)).ToList());
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
