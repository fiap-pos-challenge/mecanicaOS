using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Queries.GetPackagePriceHistory;

public sealed record GetPackagePriceHistoryQuery(Guid Id) : IRequest<Result<PriceHistoryDto>>;
