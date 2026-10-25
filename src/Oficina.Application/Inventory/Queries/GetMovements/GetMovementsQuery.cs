using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Queries.GetMovements;

public sealed record GetMovementsQuery(Guid PartId) : IRequest<Result<StockMovementListDto>>;
