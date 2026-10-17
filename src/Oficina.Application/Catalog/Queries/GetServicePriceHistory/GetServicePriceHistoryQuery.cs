using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Queries.GetServicePriceHistory;

public sealed record GetServicePriceHistoryQuery(Guid ServiceId) : IRequest<Result<PriceHistoryDto>>;
