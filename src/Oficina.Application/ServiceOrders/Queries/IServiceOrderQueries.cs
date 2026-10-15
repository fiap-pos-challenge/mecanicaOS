using Oficina.Application.Common.Pagination;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.ValueObjects;

namespace Oficina.Application.ServiceOrders.Queries;

public interface IServiceOrderQueries
{
    Task<PagedResult<ServiceOrderSummaryDto>> ListAsync(
        PageRequest page,
        ServiceOrderStatus? status,
        LicensePlate? plate,
        CancellationToken cancellationToken);

    Task<ServiceOrderDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PublicServiceOrderDto?> GetPublicByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PublicServiceOrderDto?> LookupAsync(ServiceOrderNumber number, Document document, CancellationToken cancellationToken);

    Task<AverageExecutionTimeDto> GetAverageExecutionTimeAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
}
