using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Queries.LookupServiceOrder;

public sealed record LookupServiceOrderQuery(long Number, string Document) : IRequest<Result<PublicServiceOrderDto>>;
