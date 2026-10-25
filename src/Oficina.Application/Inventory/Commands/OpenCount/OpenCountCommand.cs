using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Commands.OpenCount;

public sealed record OpenCountCommand(string Location) : IRequest<Result<StockCountDto>>;
