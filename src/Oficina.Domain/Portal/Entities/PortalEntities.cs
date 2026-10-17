namespace Oficina.Domain.Portal.Entities;

public sealed class PortalSession
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    private PortalSession(Guid id, string tokenHash, Guid estimateId, DateTimeOffset expiresAt)
    {
        Id = id;
        TokenHash = tokenHash;
        EstimateId = estimateId;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private init; }

    public string TokenHash { get; private init; }

    public Guid EstimateId { get; private init; }

    public DateTimeOffset ExpiresAt { get; private init; }

    public static PortalSession Start(string tokenHash, Guid estimateId, DateTimeOffset now) => new(Guid.CreateVersion7(), tokenHash, estimateId, now + Lifetime);
}

public sealed class PortalCharge
{
    public static readonly TimeSpan PixLifetime = TimeSpan.FromMinutes(30);

    private PortalCharge(Guid id, Guid estimateId, Guid serviceOrderId, string method, decimal amount, string txId, DateTimeOffset expiresAt)
    {
        Id = id;
        EstimateId = estimateId;
        ServiceOrderId = serviceOrderId;
        Method = method;
        Amount = amount;
        TxId = txId;
        ExpiresAt = expiresAt;
        Status = "Pending";
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

    public static PortalCharge Open(Guid estimateId, Guid serviceOrderId, string method, decimal amount, string txId, DateTimeOffset expiresAt) =>
        new(Guid.CreateVersion7(), estimateId, serviceOrderId, method, amount, txId, expiresAt);

    public string Refresh(DateTimeOffset now)
    {
        if (Status == "Pending" && ExpiresAt <= now)
        {
            Status = "Expired";
        }

        return Status;
    }

    public bool MarkPaid(DateTimeOffset now)
    {
        if (Status != "Pending")
        {
            return false;
        }

        Status = "Paid";
        PaidAt = now;
        return true;
    }
}
