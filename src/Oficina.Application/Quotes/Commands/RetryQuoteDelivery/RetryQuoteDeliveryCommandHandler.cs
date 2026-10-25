using MediatR;
using Oficina.Application.Quotes.Delivery;
using Oficina.Application.Quotes.Dtos;
using Oficina.Application.Quotes.Pricing;
using Oficina.Application.Quotes.Queries;
using Oficina.Application.Quotes.Queries.DraftQuoteMessage;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Quotes.Errors;
using Oficina.Domain.Quotes.Repositories;

namespace Oficina.Application.Quotes.Commands.RetryQuoteDelivery;

public sealed class RetryQuoteDeliveryCommandHandler(IEstimateRepository estimates, IQuoteQueries queries, QuotePricing pricing, QuoteDispatcher dispatcher, TimeProvider timeProvider)
    : IRequestHandler<RetryQuoteDeliveryCommand, Result<QuoteDeliveryDto>>
{
    public async Task<Result<QuoteDeliveryDto>> Handle(RetryQuoteDeliveryCommand command, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(command.Id, cancellationToken) is not { Delivery: { } delivery } estimate || await queries.ContextAsync(estimate.ServiceOrderId, cancellationToken) is not { } context)
        {
            return QuoteErrors.NotFound;
        }

        if (estimate.EnsureRetry(timeProvider.GetUtcNow()).Error is { } retryError)
        {
            return retryError;
        }

        var totals = estimate.PriceOf(estimate.Lines, await pricing.TermsAsync(context.Facts, cancellationToken));
        var channel = delivery.Channel == QuoteChannel.Email && !(context.ConsentEmail && context.Email is not null) ? QuoteChannel.Link : delivery.Channel;
        var sent = await dispatcher.SendAsync(command.Id, channel, DraftQuoteMessageQueryHandler.DefaultDays, QuoteTexts.Message(context, totals.Total, DraftQuoteMessageQueryHandler.DefaultDays), cancellationToken);
        return sent.Error is { } error ? error : sent.Value.Delivery;
    }
}
