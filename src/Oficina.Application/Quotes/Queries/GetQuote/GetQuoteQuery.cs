using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Queries.GetQuote;

public sealed record GetQuoteQuery(Guid Id) : IRequest<Result<QuoteDto>>;
