using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Application.Quotes.Presenters;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Quotes.Errors;
using Oficina.Domain.Quotes.Repositories;

namespace Oficina.Application.Quotes.Queries.GetQuote;

public sealed class GetQuoteQueryHandler(IEstimateRepository estimates, QuotePresenter presenter) : IRequestHandler<GetQuoteQuery, Result<QuoteDto>>
{
    public async Task<Result<QuoteDto>> Handle(GetQuoteQuery query, CancellationToken cancellationToken) =>
        await estimates.GetByIdAsync(query.Id, cancellationToken) is { } estimate ? await presenter.DtoAsync(estimate, cancellationToken) : QuoteErrors.NotFound;
}
