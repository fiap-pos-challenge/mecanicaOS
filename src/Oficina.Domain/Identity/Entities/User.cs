using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Errors;

namespace Oficina.Domain.Identity.Entities;

public sealed class User : AggregateRoot
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 120;
    public const int PasswordHashMaxLength = 255;
    public const int MaxFailedLoginAttempts = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private User(Guid id, string name, Email email, string? passwordHash, Role role, bool isActive)
        : base(id)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = isActive;
    }

    public string Name { get; private set; }

    public Email Email { get; private set; }

    public string? PasswordHash { get; private set; }

    public Role Role { get; private set; }

    public bool IsActive { get; private set; }

    public int FailedLoginAttempts { get; private set; }

    public DateTimeOffset? LockoutEndAt { get; private set; }

    public bool IsActiveAdmin => IsActive && Role == Role.Admin;

    public bool HasPendingInvitation => PasswordHash is null;

    public UserStatus Status => !IsActive
        ? UserStatus.Inactive
        : HasPendingInvitation ? UserStatus.InvitationPending : UserStatus.Active;

    public bool CanLogin => IsActive && !IsDeleted && !HasPendingInvitation;

    public static Result<User> Create(string name, Email email, string passwordHash, Role role)
    {
        if (!IsValidName(name))
        {
            return IdentityErrors.InvalidName;
        }

        if (string.IsNullOrWhiteSpace(passwordHash) || passwordHash.Length > PasswordHashMaxLength)
        {
            return IdentityErrors.InvalidPasswordHash;
        }

        return new User(Guid.CreateVersion7(), name.Trim(), email, passwordHash, role, isActive: true);
    }

    public static Result<User> Invite(string name, Email email, Role role)
    {
        if (!IsValidName(name))
        {
            return IdentityErrors.InvalidName;
        }

        return new User(Guid.CreateVersion7(), name.Trim(), email, passwordHash: null, role, isActive: true);
    }

    public Result DefinePassword(string passwordHash)
    {
        if (!HasPendingInvitation)
        {
            return IdentityErrors.InvitationAlreadyUsed;
        }

        if (string.IsNullOrWhiteSpace(passwordHash) || passwordHash.Length > PasswordHashMaxLength)
        {
            return IdentityErrors.InvalidPasswordHash;
        }

        PasswordHash = passwordHash;
        RegisterSuccessfulLogin();
        return Result.Success();
    }

    public Result ChangeName(string name)
    {
        if (!IsValidName(name))
        {
            return IdentityErrors.InvalidName;
        }

        Name = name.Trim();
        return Result.Success();
    }

    public void ChangeRole(Role role) => Role = role;

    public Result Deactivate()
    {
        if (!IsActive)
        {
            return IdentityErrors.AlreadyInactive;
        }

        IsActive = false;
        return Result.Success();
    }

    public Result Reactivate()
    {
        if (IsActive)
        {
            return IdentityErrors.AlreadyActive;
        }

        IsActive = true;
        RegisterSuccessfulLogin();
        return Result.Success();
    }

    public void Delete() => IsActive = false;

    public bool IsLockedOut(DateTimeOffset now) => LockoutEndAt > now;

    public TimeSpan LockoutRemaining(DateTimeOffset now) =>
        LockoutEndAt is { } end && end > now ? end - now : TimeSpan.Zero;

    public void RegisterFailedLogin(DateTimeOffset now)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            FailedLoginAttempts = 0;
            LockoutEndAt = now + LockoutDuration;
        }
    }

    public void RegisterSuccessfulLogin()
    {
        FailedLoginAttempts = 0;
        LockoutEndAt = null;
    }

    private static bool IsValidName(string name) =>
        !string.IsNullOrWhiteSpace(name) && name.Trim().Length is >= NameMinLength and <= NameMaxLength;
}
