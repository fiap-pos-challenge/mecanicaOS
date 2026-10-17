using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Queries.ListServiceOrders;

public sealed record ListServiceOrdersQuery(int Page, int PageSize, string? Status, string? Plate)
    : IRequest<Result<PagedResult<ServiceOrderSummaryDto>>>;
