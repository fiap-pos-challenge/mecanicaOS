using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Commands.SendPurchaseOrder;

public sealed record SendPurchaseOrderCommand(Guid Id) : IRequest<Result<PurchaseOrderDto>>;
