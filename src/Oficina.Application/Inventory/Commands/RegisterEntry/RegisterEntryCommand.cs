using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Commands.RegisterEntry;

public sealed record RegisterEntryCommand(Guid PartId, decimal Quantity, decimal UnitCost, string? Reason, Guid? SupplierId, string? InvoiceNumber) : IRequest<Result<StockMovementDto>>;
