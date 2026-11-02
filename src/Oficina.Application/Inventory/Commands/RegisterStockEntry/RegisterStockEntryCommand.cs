using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Commands.RegisterStockEntry;

public sealed record RegisterStockEntryCommand(Guid PartId, decimal Quantity, string Reason) : IRequest<Result<PartDto>>;
