using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Queries.ListCounts;

public sealed record ListCountsQuery() : IRequest<Result<StockCountListDto>>;
