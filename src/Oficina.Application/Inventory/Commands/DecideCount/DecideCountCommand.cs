using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Commands.DecideCount;

public sealed record DecideCountCommand(Guid Id, string Action, string? Reason) : IRequest<Result<StockCountDto>>;
