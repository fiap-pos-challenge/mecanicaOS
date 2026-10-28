using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.AddPartItem;

public sealed record AddPartItemCommand(Guid ServiceOrderId, Guid PartId, decimal Quantity) : IRequest<Result<ServiceOrderDetailDto>>;
