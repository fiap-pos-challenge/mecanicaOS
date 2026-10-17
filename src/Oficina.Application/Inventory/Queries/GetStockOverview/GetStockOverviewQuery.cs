using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Queries.GetStockOverview;

public sealed record GetStockOverviewQuery(int Page, int PageSize, string? Term, string? Situation, string? Location) : IRequest<Result<StockOverviewDto>>;
