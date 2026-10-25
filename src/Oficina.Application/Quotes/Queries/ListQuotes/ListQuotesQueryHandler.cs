using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Quotes.Dtos;
using Oficina.Application.Quotes.Pricing;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Quotes.Entities;

namespace Oficina.Application.Quotes.Queries.ListQuotes;

public sealed class ListQuotesQueryHandler(IQuoteQueries queries, QuotePricing pricing) : IRequestHandler<ListQuotesQuery, Result<PagedResult<QuoteSummaryDto>>>
{
    public async Task<Result<PagedResult<QuoteSummaryDto>>> Handle(ListQuotesQuery query, CancellationToken cancellationToken)
    {
        var request = new PageRequest(query.Page, query.PageSize);
        var latest = await queries.LatestAsync(query.Status, cancellationToken);
        var pageItems = latest.Skip(request.Skip).Take(request.PageSize).ToList();
        var contexts = await queries.ContextsAsync(pageItems.Select(estimate => estimate.ServiceOrderId).Distinct().ToList(), cancellationToken);
        var discounts = await pricing.DiscountsAsync(cancellationToken);
        var items = pageItems.Select(estimate =>
        {
            var context = contexts.GetValueOrDefault(estimate.ServiceOrderId);
            var totals = estimate.PriceOf(estimate.Lines, QuotePricing.Terms(discounts, context?.Facts, Domain.Quotes.Pricing.PricingTerms.NoLimit));
            return new QuoteSummaryDto(estimate.Id, estimate.Number, estimate.Version, estimate.Status, context?.CustomerName ?? string.Empty, context?.LicensePlate ?? string.Empty,
                totals.Total, estimate.Delivery?.Status, estimate.Approval?.Status == DiscountApproval.Pending, estimate.UpdatedAt ?? estimate.CreatedAt);
        }).ToList();
        return new PagedResult<QuoteSummaryDto>(items, request.Page, request.PageSize, latest.Count);
    }
}
