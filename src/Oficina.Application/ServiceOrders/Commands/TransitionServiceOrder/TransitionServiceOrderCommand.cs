using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Enums;

namespace Oficina.Application.ServiceOrders.Commands.TransitionServiceOrder;

public sealed record TransitionServiceOrderCommand(Guid Id, ServiceOrderStatus To, int Version, string? Reason)
    : IRequest<Result<ServiceOrderDetailDto>>;
