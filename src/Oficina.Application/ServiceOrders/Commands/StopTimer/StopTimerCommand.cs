using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.StopTimer;

public sealed record StopTimerCommand(Guid Id) : IRequest<Result<ServiceOrderDetailDto>>;
