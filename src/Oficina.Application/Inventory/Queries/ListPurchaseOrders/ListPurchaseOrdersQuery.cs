using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Queries.ListPurchaseOrders;

public sealed record ListPurchaseOrdersQuery() : IRequest<Result<PurchaseOrderListDto>>;
