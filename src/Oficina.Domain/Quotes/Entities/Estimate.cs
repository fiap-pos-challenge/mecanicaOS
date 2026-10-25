using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Portal.Errors;
using Oficina.Domain.Quotes.Errors;
using Oficina.Domain.Quotes.Pricing;

namespace Oficina.Domain.Quotes.Entities;

public sealed class Estimate : AggregateRoot
{
    public const int MinReasonLength = 5;
    public const int MaxPortalAttempts = 3;

    private List<EstimateLine> _lines;
    private List<string> _paymentMethods;
    private List<Guid> _acceptedLineIds;

    private Estimate(Guid id, long number, int version, Guid serviceOrderId)
        : base(id)
    {
        Number = number;
        Version = version;
        ServiceOrderId = serviceOrderId;
        Status = EstimateStatus.Pending;
        DiscountMode = Entities.DiscountMode.Automatic;
        _lines = [];
        _paymentMethods = [];
        MaxInstallments = 1;
        _acceptedLineIds = [];
    }

    public long Number { get; private set; }

    public int Version { get; private set; }

    public Guid ServiceOrderId { get; private set; }

    public string Status { get; private set; }

    public IReadOnlyCollection<EstimateLine> Lines => _lines.AsReadOnly();

    public string DiscountMode { get; private set; }

    public ManualDiscount? ManualDiscount { get; private set; }

    public IReadOnlyCollection<string> PaymentMethods => _paymentMethods.AsReadOnly();

    public int MaxInstallments { get; private set; }

    public DiscountApproval? Approval { get; private set; }

    public EstimateDelivery? Delivery { get; private set; }

    public string? DeliveryTokenHash { get; private set; }

    public int PortalFailures { get; private set; }

    public string? RejectionReason { get; private set; }

    public IReadOnlyCollection<Guid> AcceptedLineIds => _acceptedLineIds.AsReadOnly();

    public IReadOnlyCollection<EstimateLine> EffectiveLines =>
        Status == EstimateStatus.Approved ? _lines.Where(line => _acceptedLineIds.Contains(line.Id)).ToList() : Lines;

    public bool IsPortalLocked => PortalFailures >= MaxPortalAttempts;

    public int PortalAttemptsLeft => Math.Max(0, MaxPortalAttempts - PortalFailures);

    public bool IsPartiallyApproved => Status == EstimateStatus.Approved && _acceptedLineIds.Count < _lines.Count;

    public static Estimate Open(long number, Guid serviceOrderId, IReadOnlyCollection<string> methods, int maxInstallments)
    {
        var estimate = new Estimate(Guid.CreateVersion7(), number, 1, serviceOrderId);
        estimate._paymentMethods = methods.ToList();
        estimate.MaxInstallments = maxInstallments;
        return estimate;
    }

    public bool IsEditable(DateTimeOffset now) =>
        Status == EstimateStatus.Pending && (Delivery is null || Delivery.Status == DeliveryStatus.Failed || Delivery.ExpiresAt <= now);

    public Result EnsureEditable(DateTimeOffset now) => IsEditable(now) ? Result.Success() : QuoteErrors.Locked;

    public Result EnsureCanBeAnswered() => Status == EstimateStatus.Pending ? Result.Success() : QuoteErrors.NotPending;

    public Result SaveDraft(IReadOnlyCollection<EstimateLine> lines, string mode, ManualDiscount? manual, IReadOnlyCollection<string> methods, int maxInstallments, DateTimeOffset now)
    {
        if (!IsEditable(now))
        {
            return QuoteErrors.Locked;
        }

        if (lines.Any(line => !EstimatePricing.IsValidQuantity(line.Quantity, line.Fractional)))
        {
            return QuoteErrors.InvalidQuantity;
        }

        var normalizedMode = Entities.DiscountMode.Normalize(mode);
        if (normalizedMode == Entities.DiscountMode.Manual && manual is not null && manual.Value <= 0)
        {
            return QuoteErrors.InvalidDiscount;
        }

        _lines = lines.ToList();
        DiscountMode = normalizedMode;
        ManualDiscount = normalizedMode == Entities.DiscountMode.Manual ? manual : null;
        _paymentMethods = methods.ToList();
        MaxInstallments = maxInstallments;
        if (Delivery is { } delivery && (delivery.Status == DeliveryStatus.Failed || delivery.ExpiresAt <= now))
        {
            Delivery = null;
        }

        return Result.Success();
    }

    public Result RequestApproval(ManualDiscount discount, string requestedBy, PricingTerms terms, DateTimeOffset now)
    {
        if (!IsEditable(now))
        {
            return QuoteErrors.Locked;
        }

        if (!EstimatePricing.IsValidDiscount(discount.Type, discount.Value))
        {
            return QuoteErrors.InvalidDiscount;
        }

        if (discount.Reason.Trim().Length < MinReasonLength)
        {
            return QuoteErrors.DiscountReason;
        }

        if (!EstimatePricing.Price(_lines, Entities.DiscountMode.Manual, discount, terms).ExceedsLimit)
        {
            return QuoteErrors.ApprovalNotNeeded;
        }

        Approval = new DiscountApproval(Guid.CreateVersion7(), DiscountApproval.Pending, discount.Type, discount.Value, discount.Reason.Trim(), requestedBy, now, null);
        return Result.Success();
    }

    public Result DecideApproval(Guid approvalId, bool approve, string decidedBy)
    {
        if (Approval is not { } approval || approval.Id != approvalId)
        {
            return QuoteErrors.NotFound;
        }

        if (approval.Status != DiscountApproval.Pending)
        {
            return QuoteErrors.ApprovalDecided;
        }

        Approval = approval with { Status = approve ? DiscountApproval.Approved : DiscountApproval.Rejected, DecidedBy = decidedBy };
        return Result.Success();
    }

    public EstimateTotals Price(PricingTerms terms) => EstimatePricing.Price(EffectiveLines, DiscountMode, ManualDiscount, terms);

    public EstimateTotals PriceOf(IReadOnlyCollection<EstimateLine> lines, PricingTerms terms) => EstimatePricing.Price(lines, DiscountMode, ManualDiscount, terms);

    public Result EnsureReadyToSend(PricingTerms terms, DateTimeOffset now)
    {
        if (!IsEditable(now))
        {
            return QuoteErrors.Locked;
        }

        if (_lines.Count == 0)
        {
            return QuoteErrors.Empty;
        }

        var totals = EstimatePricing.Price(_lines, DiscountMode, ManualDiscount, terms);
        if (totals.Total <= 0)
        {
            return QuoteErrors.ZeroTotal;
        }

        if (_lines.Exists(line => !EstimatePricing.IsValidQuantity(line.Quantity, line.Fractional)))
        {
            return QuoteErrors.InvalidQuantity;
        }

        if (DiscountMode != Entities.DiscountMode.Manual)
        {
            return Result.Success();
        }

        if (ManualDiscount is not { } manual || !EstimatePricing.IsValidDiscount(manual.Type, manual.Value) || manual.Reason.Trim().Length < MinReasonLength)
        {
            return QuoteErrors.DiscountReason;
        }

        return totals.ExceedsLimit && Approval?.Covers(manual) != true ? QuoteErrors.ApprovalRequired : Result.Success();
    }

    public void Deliver(string channel, string link, string tokenHash, DateTimeOffset expiresAt)
    {
        Delivery = new EstimateDelivery(Guid.CreateVersion7(), channel, DeliveryStatus.Queued, link, tokenHash, expiresAt, null, null, null, (Delivery?.Attempts ?? 0) + 1);
        DeliveryTokenHash = tokenHash;
        PortalFailures = 0;
    }

    public Result EnsurePortalLinkOpen(DateTimeOffset now)
    {
        if (Delivery is not { } delivery || delivery.ExpiresAt <= now)
        {
            return PortalErrors.LinkUnavailable;
        }

        return IsPortalLocked ? PortalErrors.Locked : Result.Success();
    }

    public Result RegisterPortalAttempt(DateTimeOffset now)
    {
        var open = EnsurePortalLinkOpen(now);
        if (open.IsFailure)
        {
            return open;
        }

        PortalFailures++;
        return Result.Success();
    }

    public void ConfirmPortalIdentity(DateTimeOffset now)
    {
        PortalFailures = 0;
        MarkOpened(now);
    }

    public void MarkDelivery(bool sent, string? failureReason, DateTimeOffset now)
    {
        if (Delivery is { } delivery)
        {
            Delivery = sent
                ? delivery with { Status = DeliveryStatus.Sent, SentAt = now, FailureReason = null }
                : delivery with { Status = DeliveryStatus.Failed, FailureReason = failureReason };
        }
    }

    public void MarkOpened(DateTimeOffset now)
    {
        if (Delivery is { Status: DeliveryStatus.Sent or DeliveryStatus.Queued } delivery)
        {
            Delivery = delivery with { Status = DeliveryStatus.Opened, OpenedAt = now, SentAt = delivery.SentAt ?? now };
        }
    }

    public Result EnsureRetry(DateTimeOffset now) =>
        Status == EstimateStatus.Pending && Delivery is { } delivery && (delivery.Status == DeliveryStatus.Failed || delivery.ExpiresAt <= now)
            ? Result.Success()
            : QuoteErrors.RetryNotAllowed;

    public Result<IReadOnlyCollection<EstimateLine>> Approve(IReadOnlyCollection<Guid>? acceptedLineIds, PricingTerms terms)
    {
        if (Status != EstimateStatus.Pending)
        {
            return QuoteErrors.NotPending;
        }

        IReadOnlyCollection<EstimateLine> accepted = acceptedLineIds is null ? Lines : _lines.Where(line => acceptedLineIds.Contains(line.Id)).ToList();
        if (accepted.Count == 0)
        {
            return QuoteErrors.NoItemsAccepted;
        }

        if (EstimatePricing.Price(accepted, DiscountMode, ManualDiscount, terms).Total <= 0)
        {
            return QuoteErrors.ZeroTotal;
        }

        Status = EstimateStatus.Approved;
        _acceptedLineIds = accepted.Select(line => line.Id).ToList();
        return Result.Success(accepted);
    }

    public Result Reject(string reason)
    {
        if (Status != EstimateStatus.Pending)
        {
            return QuoteErrors.NotPending;
        }

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < MinReasonLength)
        {
            return QuoteErrors.RejectReason;
        }

        Status = EstimateStatus.Rejected;
        RejectionReason = reason.Trim();
        return Result.Success();
    }

    public Result<decimal> AmountToCharge(bool alreadyPaid, PricingTerms terms)
    {
        var due = Status == EstimateStatus.Approved && !alreadyPaid ? Price(terms).Total : 0;
        return due > 0 ? due : PortalErrors.NothingToPay;
    }

    public Result<Estimate> NextVersion()
    {
        if (Status != EstimateStatus.Rejected)
        {
            return QuoteErrors.VersionNotAllowed;
        }

        var next = new Estimate(Guid.CreateVersion7(), Number, Version + 1, ServiceOrderId)
        {
            DiscountMode = DiscountMode,
            ManualDiscount = ManualDiscount,
            MaxInstallments = MaxInstallments,
        };
        next._lines = _lines.Select(line => line with { Id = Guid.CreateVersion7() }).ToList();
        next._paymentMethods = _paymentMethods.ToList();
        return next;
    }
}
