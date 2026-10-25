using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Commands.RecordCount;

public sealed record RecordCountCommand(Guid Id, IReadOnlyList<CountLineInput> Lines) : IRequest<Result<StockCountDto>>;
