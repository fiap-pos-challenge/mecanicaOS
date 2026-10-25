using Oficina.Application.Identity.Invitations;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Portal.Errors;
using Oficina.Domain.Portal.Repositories;
using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.Quotes.Repositories;

namespace Oficina.Application.Portal;

public sealed class PortalAccess(IEstimateRepository estimates, IPortalRepository portal, TimeProvider timeProvider)
{
    public async Task<Result<Estimate>> LinkAsync(string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || await estimates.GetByDeliveryTokenAsync(InvitationToken.Hash(token.Trim()), cancellationToken) is not { } estimate)
        {
            return PortalErrors.LinkUnavailable;
        }

        var open = estimate.EnsurePortalLinkOpen(timeProvider.GetUtcNow());
        return open.Error is { } error ? error : estimate;
    }

    public async Task<Result<Estimate>> SessionAsync(string? session, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(session) || await portal.GetSessionAsync(InvitationToken.Hash(session.Trim()), cancellationToken) is not { } found
            || found.ExpiresAt <= timeProvider.GetUtcNow() || await estimates.GetByIdAsync(found.EstimateId, cancellationToken) is not { } estimate)
        {
            return PortalErrors.SessionExpired;
        }

        return await estimates.GetLatestByNumberAsync(estimate.Number, cancellationToken) ?? estimate;
    }

    public static Error ForPortal(Error error) =>
        error.Code == Domain.Quotes.Errors.QuoteErrors.NotPending.Code ? PortalErrors.AlreadyAnswered
        : error.Code == Domain.Quotes.Errors.QuoteErrors.NoItemsAccepted.Code ? PortalErrors.NoItems
        : error;
}
