using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Queries.GetPartPriceHistory;

public sealed record GetPartPriceHistoryQuery(Guid Id, bool IncludeCost) : IRequest<Result<PriceHistoryDto>>;
