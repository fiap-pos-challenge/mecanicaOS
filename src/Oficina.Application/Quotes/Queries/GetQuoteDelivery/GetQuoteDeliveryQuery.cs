using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Queries.GetQuoteDelivery;

public sealed record GetQuoteDeliveryQuery(Guid Id) : IRequest<Result<QuoteDeliveryDto>>;
