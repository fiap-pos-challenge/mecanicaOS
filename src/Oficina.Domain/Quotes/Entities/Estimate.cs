using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Quotes.Errors;

namespace Oficina.Domain.Quotes.Entities;

public sealed class Estimate : AggregateRoot
{
    public const int MinReasonLength = 5;

    private Estimate(Guid id, long number, int version, Guid serviceOrderId)
        : base(id)
    {
        Number = number;
        Version = version;
        ServiceOrderId = serviceOrderId;
        Status = EstimateStatus.Pending;
        DiscountMode = "Automatic";
        Lines = [];
        PaymentMethods = [];
        MaxInstallments = 1;
        AcceptedLineIds = [];
    }

    public long Number { get; private set; }

    public int Version { get; private set; }

    public Guid ServiceOrderId { get; private set; }

    public string Status { get; private set; }

    public List<EstimateLine> Lines { get; private set; }

    public string DiscountMode { get; private set; }

    public ManualDiscount? ManualDiscount { get; private set; }

    public List<string> PaymentMethods { get; private set; }

    public int MaxInstallments { get; private set; }

    public DiscountApproval? Approval { get; private set; }

    public EstimateDelivery? Delivery { get; private set; }

    public string? DeliveryTokenHash { get; private set; }

    public int PortalFailures { get; private set; }

    public string? RejectionReason { get; private set; }

    public List<Guid> AcceptedLineIds { get; private set; }

    public static Estimate Open(long number, Guid serviceOrderId, IReadOnlyCollection<string> methods, int maxInstallments) =>
        new(Guid.CreateVersion7(), number, 1, serviceOrderId) { PaymentMethods = methods.ToList(), MaxInstallments = maxInstallments };

    public bool IsEditable(DateTimeOffset now) =>
        Status == EstimateStatus.Pending && (Delivery is null || Delivery.Status == DeliveryStatus.Failed || Delivery.ExpiresAt <= now);

    public Result SaveDraft(IReadOnlyCollection<EstimateLine> lines, string mode, ManualDiscount? manual, IReadOnlyCollection<string> methods, int maxInstallments, DateTimeOffset now)
    {
        if (!IsEditable(now))
        {
            return QuoteErrors.Locked;
        }

        Lines = lines.ToList();
        DiscountMode = mode;
        ManualDiscount = mode == "Manual" ? manual : null;
        PaymentMethods = methods.ToList();
        MaxInstallments = maxInstallments;
        if (Delivery is { } delivery && (delivery.Status == DeliveryStatus.Failed || delivery.ExpiresAt <= now))
        {
            Delivery = null;
        }

        return Result.Success();
    }

    public Result RequestApproval(string type, decimal value, string reason, string requestedBy, DateTimeOffset now)
    {
        if (!IsEditable(now))
        {
            return QuoteErrors.Locked;
        }

        Approval = new DiscountApproval(Guid.CreateVersion7(), "Pending", type, value, reason.Trim(), requestedBy, now, null);
        return Result.Success();
    }

    public Result DecideApproval(Guid approvalId, bool approve, string decidedBy)
    {
        if (Approval is not { } approval || approval.Id != approvalId)
        {
            return QuoteErrors.NotFound;
        }

        if (approval.Status != "Pending")
        {
            return QuoteErrors.ApprovalDecided;
        }

        Approval = approval with { Status = approve ? "Approved" : "Rejected", DecidedBy = decidedBy };
        return Result.Success();
    }

    public void Deliver(string channel, string link, string tokenHash, DateTimeOffset expiresAt)
    {
        Delivery = new EstimateDelivery(Guid.CreateVersion7(), channel, DeliveryStatus.Queued, link, tokenHash, expiresAt, null, null, null, (Delivery?.Attempts ?? 0) + 1);
        DeliveryTokenHash = tokenHash;
        PortalFailures = 0;
    }

    public void RegisterPortalFailure() => PortalFailures++;

    public void ResetPortalFailures() => PortalFailures = 0;

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

    public Result Approve(IReadOnlyCollection<Guid>? acceptedLineIds = null)
    {
        if (Status != EstimateStatus.Pending)
        {
            return QuoteErrors.NotPending;
        }

        Status = EstimateStatus.Approved;
        AcceptedLineIds = (acceptedLineIds ?? Lines.Select(line => line.Id).ToList()).ToList();
        return Result.Success();
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

    public Result<Estimate> NextVersion()
    {
        if (Status != EstimateStatus.Rejected)
        {
            return QuoteErrors.VersionNotAllowed;
        }

        return new Estimate(Guid.CreateVersion7(), Number, Version + 1, ServiceOrderId)
        {
            Lines = Lines.Select(line => line with { Id = Guid.CreateVersion7() }).ToList(),
            DiscountMode = DiscountMode,
            ManualDiscount = ManualDiscount,
            PaymentMethods = PaymentMethods.ToList(),
            MaxInstallments = MaxInstallments,
        };
    }
}
