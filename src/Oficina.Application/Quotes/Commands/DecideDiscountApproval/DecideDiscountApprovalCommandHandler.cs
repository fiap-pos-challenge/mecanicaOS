using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Quotes.Dtos;
using Oficina.Application.Quotes.Presenters;
using Oficina.Application.Quotes.Pricing;
using Oficina.Domain.Audit.Entities;
using Oficina.Domain.Audit.Repositories;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Quotes.Errors;
using Oficina.Domain.Quotes.Repositories;

namespace Oficina.Application.Quotes.Commands.DecideDiscountApproval;

public sealed class DecideDiscountApprovalCommandHandler(
    IEstimateRepository estimates,
    QuotePricing pricing,
    IAuditRepository audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<DecideDiscountApprovalCommand, Result<DiscountApprovalDto>>
{
    public const string AuditArea = "discounts";

    public async Task<Result<DiscountApprovalDto>> Handle(DecideDiscountApprovalCommand command, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(command.Id, cancellationToken) is not { } estimate)
        {
            return QuoteErrors.NotFound;
        }

        var name = await pricing.CurrentNameAsync(cancellationToken);
        if (estimate.DecideApproval(command.ApprovalId, command.Approve, name).Error is { } decideError)
        {
            return decideError;
        }

        audit.Add(AuditEntry.Record(timeProvider.GetUtcNow(), currentUser.UserId, name, AuditArea, command.Approve ? QuoteTexts.DiscountApproved : QuoteTexts.DiscountRejected,
            QuoteTexts.QuoteReference(estimate.Number), estimate.Approval?.Reason ?? string.Empty));
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return estimate.Approval is { } approval ? QuotePresenter.Approval(approval) : QuoteErrors.NotFound;
    }
}
