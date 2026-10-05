using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.ValueObjects;

namespace Oficina.Domain.ServiceOrders.Entities;

public sealed class Quote : Entity
{
    private Quote(
        Guid id,
        Guid serviceOrderId,
        int version,
        QuoteStatus status,
        Money servicesTotal,
        Money partsTotal,
        Money total,
        DateTimeOffset sentAt)
        : base(id)
    {
        ServiceOrderId = serviceOrderId;
        Version = version;
        Status = status;
        ServicesTotal = servicesTotal;
        PartsTotal = partsTotal;
        Total = total;
        SentAt = sentAt;
    }

    public Guid ServiceOrderId { get; private set; }

    public int Version { get; private set; }

    public QuoteStatus Status { get; private set; }

    public Money ServicesTotal { get; private set; }

    public Money PartsTotal { get; private set; }

    public Money Total { get; private set; }

    public ApprovalToken? ApprovalToken { get; private set; }

    public DateTimeOffset SentAt { get; private set; }

    public DateTimeOffset? RespondedAt { get; private set; }

    public string? RejectionReason { get; private set; }

    internal static Quote Send(
        Guid serviceOrderId,
        int version,
        Money servicesTotal,
        Money partsTotal,
        ApprovalToken approvalToken,
        DateTimeOffset sentAt)
    {
        var quote = new Quote(
            Guid.CreateVersion7(),
            serviceOrderId,
            version,
            QuoteStatus.Pending,
            servicesTotal,
            partsTotal,
            servicesTotal.Add(partsTotal),
            sentAt);
        quote.ApprovalToken = approvalToken;
        return quote;
    }

    internal void ReplaceApprovalToken(ApprovalToken approvalToken) => ApprovalToken = approvalToken;

    internal void Approve(DateTimeOffset respondedAt)
    {
        Status = QuoteStatus.Approved;
        RespondedAt = respondedAt;
        ApprovalToken = null;
    }

    internal void Reject(string reason, DateTimeOffset respondedAt)
    {
        Status = QuoteStatus.Rejected;
        RejectionReason = reason;
        RespondedAt = respondedAt;
        ApprovalToken = null;
    }
}
