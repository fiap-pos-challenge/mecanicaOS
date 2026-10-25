using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Commands.CreatePurchaseOrder;

public sealed record CreatePurchaseOrderCommand(Guid SupplierId, IReadOnlyList<PurchaseOrderLineInput> Lines) : IRequest<Result<PurchaseOrderDto>>;
