using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Application.Quotes.Pricing;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Quotes.Errors;
using Oficina.Domain.Quotes.Repositories;

namespace Oficina.Application.Quotes.Queries.DraftQuoteMessage;

public sealed class DraftQuoteMessageQueryHandler(IEstimateRepository estimates, IQuoteQueries queries, QuotePricing pricing) : IRequestHandler<DraftQuoteMessageQuery, Result<MessageDraftDto>>
{
    public const int DefaultDays = 7;

    public async Task<Result<MessageDraftDto>> Handle(DraftQuoteMessageQuery query, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(query.Id, cancellationToken) is not { } estimate || await queries.ContextAsync(estimate.ServiceOrderId, cancellationToken) is not { } context)
        {
            return QuoteErrors.NotFound;
        }

        var totals = estimate.PriceOf(estimate.Lines, await pricing.TermsAsync(context.Facts, cancellationToken));
        return new MessageDraftDto(QuoteTexts.Message(context, totals.Total, DefaultDays));
    }
}
