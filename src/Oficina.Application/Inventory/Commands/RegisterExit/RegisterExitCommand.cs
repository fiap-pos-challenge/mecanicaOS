using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Commands.RegisterExit;

public sealed record RegisterExitCommand(Guid PartId, decimal Quantity, string? Reason, long? ServiceOrderNumber) : IRequest<Result<StockMovementDto>>;
