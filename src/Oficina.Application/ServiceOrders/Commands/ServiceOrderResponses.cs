using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.Application.ServiceOrders.Commands;

public static class ServiceOrderResponses
{
    public static async Task<Result<ServiceOrderDetailDto>> DetailAsync(IServiceOrderQueries queries, Guid id, CancellationToken cancellationToken) =>
        await queries.GetByIdAsync(id, cancellationToken) is { } detail ? detail : ServiceOrderErrors.NotFound;

    public static async Task<Result<MechanicOrderDto>> WorkAsync(IServiceOrderBoardQueries queries, Guid id, CancellationToken cancellationToken) =>
        await queries.GetWorkAsync(id, cancellationToken) is { } work ? work : ServiceOrderErrors.NotFound;
}
