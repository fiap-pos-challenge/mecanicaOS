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
