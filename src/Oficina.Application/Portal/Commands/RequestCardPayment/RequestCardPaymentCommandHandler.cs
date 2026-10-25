using MediatR;
using Oficina.Application.Portal.Dtos;
using Oficina.Application.Quotes.Pricing;
using Oficina.Application.Quotes.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Portal.Errors;
using Oficina.Domain.Portal.Repositories;

namespace Oficina.Application.Portal.Commands.RequestCardPayment;

public sealed class RequestCardPaymentCommandHandler(PortalAccess access, IPortalRepository portal, IQuoteQueries quoteQueries, QuotePricing pricing)
    : IRequestHandler<RequestCardPaymentCommand, Result<CardRedirectDto>>
{
    public async Task<Result<CardRedirectDto>> Handle(RequestCardPaymentCommand command, CancellationToken cancellationToken)
    {
        var found = await access.SessionAsync(command.Session, cancellationToken);
        if (found.Error is { } sessionError)
        {
            return sessionError;
        }

        var estimate = found.Value;
        var context = await quoteQueries.ContextAsync(estimate.ServiceOrderId, cancellationToken);
        var due = estimate.AmountToCharge(await portal.PaidAtAsync(estimate.ServiceOrderId, cancellationToken) is not null, await pricing.TermsAsync(context?.Facts, cancellationToken));
        return due.Error ?? PortalErrors.CardUnavailable;
    }
}
