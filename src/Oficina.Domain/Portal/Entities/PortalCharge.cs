using Oficina.Domain.Common.Events;
using Oficina.Domain.Portal.Events;

namespace Oficina.Domain.Portal.Entities;

public sealed class PortalCharge : IHasDomainEvents
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Expired = "Expired";
    public const string PixMethod = "Pix";

    public static readonly TimeSpan PixLifetime = TimeSpan.FromMinutes(30);

    private readonly List<IDomainEvent> _domainEvents;

    private PortalCharge(Guid id, Guid estimateId, Guid serviceOrderId, string method, decimal amount, string txId, DateTimeOffset expiresAt)
    {
        Id = id;
        EstimateId = estimateId;
        ServiceOrderId = serviceOrderId;
        Method = method;
        Amount = amount;
        TxId = txId;
        ExpiresAt = expiresAt;
        Status = Pending;
        _domainEvents = new List<IDomainEvent>();
    }

    public Guid Id { get; private init; }

    public Guid EstimateId { get; private init; }

    public Guid ServiceOrderId { get; private init; }

    public string Method { get; private init; }

    public decimal Amount { get; private init; }

    public string TxId { get; private init; }

    public DateTimeOffset ExpiresAt { get; private init; }

    public string Status { get; private set; }

    public DateTimeOffset? PaidAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static PortalCharge Open(Guid estimateId, Guid serviceOrderId, string method, decimal amount, string txId, DateTimeOffset expiresAt) =>
        new(Guid.CreateVersion7(), estimateId, serviceOrderId, method, amount, txId, expiresAt);

    public string Refresh(DateTimeOffset now)
    {
        if (Status == Pending && ExpiresAt <= now)
        {
            Status = Expired;
        }

        return Status;
    }

    public bool MarkPaid(DateTimeOffset now)
    {
        if (Status != Pending)
        {
            return false;
        }

        Status = Paid;
        PaidAt = now;
        _domainEvents.Add(new PaymentConfirmed(Id, EstimateId, ServiceOrderId, Method, Amount, now));
        return true;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
