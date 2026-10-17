namespace Oficina.Domain.Identity.Entities;

public sealed class UserSession
{
    public const int TokenHashLength = 64;
    public const int DeviceMaxLength = 120;

    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    private UserSession(Guid id, Guid userId, string tokenHash, string device, DateTimeOffset createdAt, DateTimeOffset lastSeenAt, DateTimeOffset expiresAt)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        Device = device;
        CreatedAt = createdAt;
        LastSeenAt = lastSeenAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private init; }

    public Guid UserId { get; private init; }

    public string TokenHash { get; private set; }

    public string Device { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset LastSeenAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    public static UserSession Start(Guid userId, string tokenHash, string? device, DateTimeOffset now) =>
        new(Guid.CreateVersion7(), userId, tokenHash, NormalizeDevice(device), now, now, now + Lifetime);

    public void Rotate(string tokenHash, DateTimeOffset now)
    {
        TokenHash = tokenHash;
        LastSeenAt = now;
        ExpiresAt = now + Lifetime;
    }

    public void Revoke(DateTimeOffset now)
    {
        if (RevokedAt is null)
        {
            RevokedAt = now;
        }
    }

    private static string NormalizeDevice(string? device)
    {
        var value = string.IsNullOrWhiteSpace(device) ? "Navegador" : device.Trim();
        return value.Length > DeviceMaxLength ? value[..DeviceMaxLength] : value;
    }
}
