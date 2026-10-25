using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Invitations;
using Oficina.Application.Quotes.Pricing;
using Oficina.Application.Quotes.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.Quotes.Repositories;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;
using Oficina.Domain.ServiceOrders.Services;
using Oficina.Domain.ServiceOrders.ValueObjects;

namespace Oficina.Application.Quotes.Decisions;

public sealed class QuoteDecider(
    IEstimateRepository estimates,
    IServiceOrderRepository serviceOrders,
    IQuoteQueries queries,
    QuotePricing pricing,
    EstimateScopeResolver scopes,
    QuoteApprovalPolicy approvalPolicy,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<Result> ApproveAsync(Estimate estimate, IReadOnlyCollection<Guid>? acceptedLineIds, CancellationToken cancellationToken)
    {
        if (await serviceOrders.GetByIdAsync(estimate.ServiceOrderId, cancellationToken) is not { } order)
        {
            return ServiceOrderErrors.NotFound;
        }

        var context = await queries.ContextAsync(estimate.ServiceOrderId, cancellationToken);
        var terms = await pricing.TermsAsync(context?.Facts, cancellationToken);
        var accepted = estimate.Approve(acceptedLineIds, terms);
        if (accepted.Error is { } approveError)
        {
            return approveError;
        }

        var scope = await scopes.ResolveAsync(accepted.Value, estimate.Price(terms).Total, cancellationToken);
        return await approvalPolicy.ApproveAsync(order, scope, NewToken(), currentUser.UserId, timeProvider.GetUtcNow(), cancellationToken);
    }

    public async Task<Result> RejectAsync(Estimate estimate, string reason, CancellationToken cancellationToken)
    {
        if (await serviceOrders.GetByIdAsync(estimate.ServiceOrderId, cancellationToken) is not { } order)
        {
            return ServiceOrderErrors.NotFound;
        }

        var rejected = estimate.Reject(reason);
        return rejected.IsFailure ? rejected : order.RejectQuoteAnswer(reason, currentUser.UserId, timeProvider.GetUtcNow());
    }

    public async Task<Result> ApproveServiceOrderAsync(ServiceOrder order, Guid? approvedByUserId, CancellationToken cancellationToken) =>
        await PendingEstimateAsync(order, cancellationToken) is { } estimate
            ? await ApproveAsync(estimate, null, cancellationToken)
            : await approvalPolicy.ApproveAsync(order, approvedByUserId, timeProvider.GetUtcNow(), cancellationToken);

    public async Task<Result> RejectServiceOrderAsync(ServiceOrder order, string reason, CancellationToken cancellationToken) =>
        await PendingEstimateAsync(order, cancellationToken) is { } estimate
            ? await RejectAsync(estimate, reason, cancellationToken)
            : order.RejectQuote(reason, currentUser.UserId, timeProvider.GetUtcNow());

    public async Task<Result> SubmitAsync(Estimate estimate, decimal total, CancellationToken cancellationToken)
    {
        if (await serviceOrders.GetByIdAsync(estimate.ServiceOrderId, cancellationToken) is not { } order)
        {
            return ServiceOrderErrors.NotFound;
        }

        var scope = await scopes.ResolveAsync(estimate.Lines, total, cancellationToken);
        return order.SubmitQuote(scope, NewToken(), currentUser.UserId, timeProvider.GetUtcNow());
    }

    private static ApprovalToken NewToken() => ApprovalToken.FromRawToken(InvitationToken.Generate()).Value;

    private async Task<Estimate?> PendingEstimateAsync(ServiceOrder order, CancellationToken cancellationToken) =>
        await estimates.GetLatestByServiceOrderAsync(order.Id, cancellationToken) is { Status: EstimateStatus.Pending } estimate ? estimate : null;
}
