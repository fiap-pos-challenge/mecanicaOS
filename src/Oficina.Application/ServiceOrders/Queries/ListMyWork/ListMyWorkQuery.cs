using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Queries.ListMyWork;

public sealed record ListMyWorkQuery : IRequest<Result<MechanicOrderListDto>>;
