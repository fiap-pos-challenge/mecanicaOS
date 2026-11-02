using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.AddServiceItem;

public sealed record AddServiceItemCommand(Guid ServiceOrderId, Guid ServiceId, int Quantity) : IRequest<Result<ServiceOrderDetailDto>>;
