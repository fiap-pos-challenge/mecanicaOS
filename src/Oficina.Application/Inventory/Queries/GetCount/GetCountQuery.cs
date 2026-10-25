using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Queries.GetCount;

public sealed record GetCountQuery(Guid Id) : IRequest<Result<StockCountDto>>;
