using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.CompleteServiceOrder;

public sealed record CompleteServiceOrderCommand(Guid ServiceOrderId) : IRequest<Result<ServiceOrderDetailDto>>;
