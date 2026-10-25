namespace Oficina.Domain.Identity.Entities;

public sealed class TwoFactorChallenge
{
    public const int MaxAttempts = 5;

    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private TwoFactorChallenge(Guid id, Guid userId, DateTimeOffset expiresAt)
    {
        Id = id;
        UserId = userId;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private init; }

    public Guid UserId { get; private init; }

    public DateTimeOffset ExpiresAt { get; private init; }

    public int Attempts { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public bool IsOpen(DateTimeOffset now) => CompletedAt is null && ExpiresAt > now && Attempts < MaxAttempts;

    public static TwoFactorChallenge Open(Guid userId, DateTimeOffset now) => new(Guid.CreateVersion7(), userId, now + Lifetime);

    public void RegisterFailure() => Attempts++;

    public void Complete(DateTimeOffset now) => CompletedAt = now;
}
