using System.Security.Cryptography;
using System.Text;
using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Identity.Invitations;
using Oficina.Application.Portal.Dtos;
using Oficina.Application.Quotes.Queries;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Portal.Entities;
using Oficina.Domain.Portal.Errors;
using Oficina.Domain.Portal.Repositories;

namespace Oficina.Application.Portal.Commands.ConfirmPortalIdentity;

public sealed class ConfirmPortalIdentityCommandHandler(PortalAccess access, IPortalRepository portal, IQuoteQueries quoteQueries, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IRequestHandler<ConfirmPortalIdentityCommand, Result<PortalConfirmDto>>
{
    public const int DigitsChecked = 3;

    public async Task<Result<PortalConfirmDto>> Handle(ConfirmPortalIdentityCommand command, CancellationToken cancellationToken)
    {
        var found = await access.LinkAsync(command.Token, cancellationToken);
        if (found.Error is { } linkError)
        {
            return linkError;
        }

        var estimate = found.Value;
        var now = timeProvider.GetUtcNow();
        if (estimate.RegisterPortalAttempt(now).Error is { } attemptError)
        {
            return attemptError;
        }

        if (await CommitAsync(cancellationToken) is { } attemptCommitError)
        {
            return attemptCommitError;
        }

        var context = await quoteQueries.ContextAsync(estimate.ServiceOrderId, cancellationToken);
        if (context?.Phone is not { Length: >= DigitsChecked } phone
            || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(phone[^DigitsChecked..]), Encoding.ASCII.GetBytes((command.LastDigits ?? string.Empty).Trim())))
        {
            return estimate.IsPortalLocked ? PortalErrors.Locked : new PortalConfirmDto(PortalConfirmDto.WrongDigits, null, estimate.PortalAttemptsLeft, null);
        }

        estimate.ConfirmPortalIdentity(now);
        var sessionToken = InvitationToken.Generate();
        var session = PortalSession.Start(InvitationToken.Hash(sessionToken), estimate.Id, now);
        portal.Add(session);
        if (await CommitAsync(cancellationToken) is { } confirmCommitError)
        {
            return confirmCommitError;
        }

        return new PortalConfirmDto(PortalConfirmDto.Confirmed, sessionToken, estimate.PortalAttemptsLeft, session.ExpiresAt);
    }

    private async Task<Error?> CommitAsync(CancellationToken cancellationToken)
    {
        var committed = await unitOfWork.CommitAsync(cancellationToken);
        return committed.Error is { } error ? (error.Code == CommonErrors.ConcurrencyConflict.Code ? PortalErrors.TryAgain : error) : null;
    }
}
