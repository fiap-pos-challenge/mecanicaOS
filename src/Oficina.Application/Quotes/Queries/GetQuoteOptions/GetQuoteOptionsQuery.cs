using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Queries.GetQuoteOptions;

public sealed record GetQuoteOptionsQuery(Guid Id) : IRequest<Result<QuoteOptionsDto>>;
