using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Errors;

namespace Oficina.Domain.Identity.Entities;

public sealed class UserInvitation
{
    public const int TokenHashLength = 64;

    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(72);

    private UserInvitation(Guid id, Guid userId, string tokenHash, Guid? createdBy, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private init; }

    public Guid UserId { get; private init; }

    public string TokenHash { get; private init; }

    public Guid? CreatedBy { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset ExpiresAt { get; private init; }

    public DateTimeOffset? AcceptedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsOpen => AcceptedAt is null && RevokedAt is null;

    public static UserInvitation Issue(Guid userId, string tokenHash, Guid? createdBy, DateTimeOffset now) =>
        new(Guid.CreateVersion7(), userId, tokenHash, createdBy, now, now + Lifetime);

    public Result EnsureUsable(DateTimeOffset now)
    {
        if (!IsOpen)
        {
            return IdentityErrors.InvitationAlreadyUsed;
        }

        return ExpiresAt <= now ? IdentityErrors.InvitationExpired : Result.Success();
    }

    public Result Accept(DateTimeOffset now)
    {
        var usable = EnsureUsable(now);
        if (usable.IsFailure)
        {
            return usable;
        }

        AcceptedAt = now;
        return Result.Success();
    }

    public void Revoke(DateTimeOffset now)
    {
        if (IsOpen)
        {
            RevokedAt = now;
        }
    }
}
