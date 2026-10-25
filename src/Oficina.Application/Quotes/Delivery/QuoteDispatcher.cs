using System.Net;
using Oficina.Application.Abstractions.Links;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Documents;
using Oficina.Application.Identity.Invitations;
using Oficina.Application.Quotes.Decisions;
using Oficina.Application.Quotes.Dtos;
using Oficina.Application.Quotes.Presenters;
using Oficina.Application.Quotes.Pricing;
using Oficina.Application.Quotes.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Quotes.Errors;
using Oficina.Domain.Quotes.Repositories;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Application.Quotes.Delivery;

public sealed class QuoteDispatcher(
    IEstimateRepository estimates,
    IServiceOrderRepository serviceOrders,
    IQuoteQueries queries,
    QuotePricing pricing,
    QuoteDecider decider,
    DocumentIssuer documents,
    IPublicLinks links,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public const int MaxMessageLength = 1000;

    public async Task<Result<SendQuoteResultDto>> SendAsync(Guid id, string channel, int expiresInDays, string message, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(id, cancellationToken) is not { } estimate || await queries.ContextAsync(estimate.ServiceOrderId, cancellationToken) is not { } context)
        {
            return QuoteErrors.NotFound;
        }

        if ((channel == QuoteChannel.WhatsApp && !(context.ConsentWhatsApp && context.Phone is not null)) || (channel == QuoteChannel.Email && !(context.ConsentEmail && context.Email is not null)))
        {
            return QuoteErrors.NoConsent;
        }

        var now = timeProvider.GetUtcNow();
        var terms = await pricing.TermsWithUserLimitAsync(context.Facts, cancellationToken);
        if (estimate.EnsureReadyToSend(terms, now).Error is { } blocked)
        {
            return blocked;
        }

        var total = estimate.PriceOf(estimate.Lines, terms).Total;
        if ((await decider.SubmitAsync(estimate, total, cancellationToken)).Error is { } submitError)
        {
            return submitError;
        }

        var token = InvitationToken.Generate();
        var link = links.Portal(token);
        estimate.Deliver(channel, link, InvitationToken.Hash(token), now.AddDays(expiresInDays));
        if (await serviceOrders.GetByIdAsync(estimate.ServiceOrderId, cancellationToken) is { } order)
        {
            var vehicle = $"{context.LicensePlate} · {context.Vehicle}";
            await documents.IssueAsync(DocumentIssuer.ServiceOrderKind, order, context.CustomerName, vehicle, null, null, null, cancellationToken);
            await documents.IssueAsync(DocumentIssuer.QuoteKind, order, context.CustomerName, vehicle, estimate, estimate.Price(await pricing.TermsAsync(context.Facts, cancellationToken)), null, cancellationToken);
        }

        if (channel != QuoteChannel.Email)
        {
            estimate.MarkDelivery(true, null, now);
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        var body = message.Contains(link, StringComparison.Ordinal) ? message : $"{message.Trim()} {link}";
        if (channel == QuoteChannel.Email)
        {
            var sent = await emailSender.SendAsync(new EmailMessage(context.Email!, QuoteTexts.EmailSubject(estimate.Number, context.Vehicle), $"<p>{WebUtility.HtmlEncode(body)}</p><p><a href=\"{link}\">{QuoteTexts.ApproveLink}</a></p>"), cancellationToken);
            estimate.MarkDelivery(sent, sent ? null : QuoteTexts.EmailFailed, timeProvider.GetUtcNow());
            if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } markError)
            {
                return markError;
            }
        }

        var whatsApp = channel == QuoteChannel.WhatsApp ? $"https://wa.me/55{context.Phone}?text={Uri.EscapeDataString(body)}" : null;
        return estimate.Delivery is { } delivery ? new SendQuoteResultDto(QuotePresenter.Delivery(delivery), whatsApp) : QuoteErrors.NoDelivery;
    }
}
