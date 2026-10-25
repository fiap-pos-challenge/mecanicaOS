using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Queries.ListQuoteVersions;

public sealed record ListQuoteVersionsQuery(Guid Id) : IRequest<Result<QuoteVersionListDto>>;
