using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Application.Quotes.Presenters;
using Oficina.Application.Quotes.Pricing;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Quotes.Errors;
using Oficina.Domain.Quotes.Repositories;

namespace Oficina.Application.Quotes.Queries.ListQuoteVersions;

public sealed class ListQuoteVersionsQueryHandler(IEstimateRepository estimates, IQuoteQueries queries, QuotePricing pricing) : IRequestHandler<ListQuoteVersionsQuery, Result<QuoteVersionListDto>>
{
    public async Task<Result<QuoteVersionListDto>> Handle(ListQuoteVersionsQuery query, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(query.Id, cancellationToken) is not { } estimate)
        {
            return QuoteErrors.NotFound;
        }

        var context = await queries.ContextAsync(estimate.ServiceOrderId, cancellationToken);
        var terms = await pricing.TermsAsync(context?.Facts, cancellationToken);
        var versions = await queries.VersionsAsync(estimate.Number, cancellationToken);
        return new QuoteVersionListDto(versions.Select(version =>
        {
            var totals = version.PriceOf(version.Lines, terms);
            return new QuoteVersionDto(version.Id, version.Version, version.Status, version.Lines.Select(QuotePresenter.Line).ToList(), totals.Services, totals.Parts, totals.Discount, totals.Total, version.RejectionReason, version.CreatedAt);
        }).ToList());
    }
}
