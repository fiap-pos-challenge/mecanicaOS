using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.FinishService;

public sealed record FinishServiceCommand(Guid Id) : IRequest<Result<MechanicOrderDto>>;
