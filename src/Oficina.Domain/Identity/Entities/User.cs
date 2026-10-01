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

    private User(Guid id, string name, Email email, string passwordHash, Role role, bool isActive)
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

    public string PasswordHash { get; private set; }

    public Role Role { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsActiveAdmin => IsActive && Role == Role.Admin;

    public bool CanLogin => IsActive && !IsDeleted;

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
        return Result.Success();
    }

    public void Delete() => IsActive = false;

    private static bool IsValidName(string name) =>
        !string.IsNullOrWhiteSpace(name) && name.Trim().Length is >= NameMinLength and <= NameMaxLength;
}
