using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Application.ServiceOrders.Commands;

public sealed class ServiceOrderChanges(IServiceOrderRepository serviceOrders, IServiceOrderQueries queries, IUnitOfWork unitOfWork)
{
    public async Task<Result<ServiceOrderDetailDto>> ApplyAsync(
        Guid serviceOrderId,
        Func<ServiceOrder, Result> change,
        CancellationToken cancellationToken)
    {
        if (await serviceOrders.GetByIdAsync(serviceOrderId, cancellationToken) is not { } serviceOrder)
        {
            return ServiceOrderErrors.NotFound;
        }

        if (change(serviceOrder).Error is { } changeError)
        {
            return changeError;
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        if (await queries.GetByIdAsync(serviceOrder.Id, cancellationToken) is not { } detail)
        {
            return ServiceOrderErrors.NotFound;
        }

        return detail;
    }
}
