using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.StartService;

public sealed record StartServiceCommand(Guid Id) : IRequest<Result<MechanicOrderDto>>;
