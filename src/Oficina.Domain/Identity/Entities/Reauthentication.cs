namespace Oficina.Domain.Identity.Entities;

public sealed class Reauthentication
{
    public const int TokenHashLength = 64;

    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private Reauthentication(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAt)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private init; }

    public Guid UserId { get; private init; }

    public string TokenHash { get; private init; }

    public DateTimeOffset ExpiresAt { get; private init; }

    public DateTimeOffset? UsedAt { get; private set; }

    public static Reauthentication Issue(Guid userId, string tokenHash, DateTimeOffset now) => new(Guid.CreateVersion7(), userId, tokenHash, now + Lifetime);

    public bool TryConsume(Guid userId, DateTimeOffset now)
    {
        if (UserId != userId || UsedAt is not null || ExpiresAt <= now)
        {
            return false;
        }

        UsedAt = now;
        return true;
    }
}
