using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Application.ServiceOrders.Commands.SaveDiagnosis;

public sealed class SaveDiagnosisCommandHandler(
    IServiceOrderRepository serviceOrders,
    IServiceOrderBoardQueries queries,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<SaveDiagnosisCommand, Result<MechanicOrderDto>>
{
    public async Task<Result<MechanicOrderDto>> Handle(SaveDiagnosisCommand command, CancellationToken cancellationToken)
    {
        if (await serviceOrders.GetByIdAsync(command.Id, cancellationToken) is not { } order)
        {
            return ServiceOrderErrors.NotFound;
        }

        var now = timeProvider.GetUtcNow();
        order.WriteDiagnosis(command.Description);
        var result = Result.Success();
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
