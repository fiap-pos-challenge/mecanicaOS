using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Application.Quotes.Presenters;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Quotes.Errors;
using Oficina.Domain.Quotes.Repositories;

namespace Oficina.Application.Quotes.Queries.GetQuoteDelivery;

public sealed class GetQuoteDeliveryQueryHandler(IEstimateRepository estimates) : IRequestHandler<GetQuoteDeliveryQuery, Result<QuoteDeliveryDto>>
{
    public async Task<Result<QuoteDeliveryDto>> Handle(GetQuoteDeliveryQuery query, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(query.Id, cancellationToken) is not { } estimate)
        {
            return QuoteErrors.NotFound;
        }

        return estimate.Delivery is { } delivery ? QuotePresenter.Delivery(delivery) : QuoteErrors.NoDelivery;
    }
}
