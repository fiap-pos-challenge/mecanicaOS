using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Queries.ListQuotes;

public sealed record ListQuotesQuery(int Page, int PageSize, string? Status) : IRequest<Result<PagedResult<QuoteSummaryDto>>>;
