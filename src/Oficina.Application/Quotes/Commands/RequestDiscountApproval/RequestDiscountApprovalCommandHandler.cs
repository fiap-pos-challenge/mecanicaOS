using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Notifications;
using Oficina.Application.Quotes.Dtos;
using Oficina.Application.Quotes.Presenters;
using Oficina.Application.Quotes.Pricing;
using Oficina.Application.Quotes.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.Quotes.Errors;
using Oficina.Domain.Quotes.Repositories;

namespace Oficina.Application.Quotes.Commands.RequestDiscountApproval;

public sealed class RequestDiscountApprovalCommandHandler(
    IEstimateRepository estimates,
    IQuoteQueries queries,
    QuotePricing pricing,
    ITeamNotificationPublisher notifications,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<RequestDiscountApprovalCommand, Result<DiscountApprovalDto>>
{
    public async Task<Result<DiscountApprovalDto>> Handle(RequestDiscountApprovalCommand command, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(command.Id, cancellationToken) is not { } estimate)
        {
            return QuoteErrors.NotFound;
        }

        var context = await queries.ContextAsync(estimate.ServiceOrderId, cancellationToken);
        var terms = await pricing.TermsWithUserLimitAsync(context?.Facts, cancellationToken);
        var discount = new ManualDiscount(command.Type, command.Value, command.Reason);
        var requested = estimate.RequestApproval(discount, await pricing.CurrentNameAsync(cancellationToken), terms, timeProvider.GetUtcNow());
        if (requested.Error is { } requestError)
        {
            return requestError;
        }

        await notifications.PublishAsync(NotificationTypes.DiscountApproval, QuoteTexts.DiscountTitle(estimate.Number), discount.Reason.Trim(), "/quotes", false, null, cancellationToken);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return estimate.Approval is { } approval ? QuotePresenter.Approval(approval) : QuoteErrors.NotFound;
    }
}
