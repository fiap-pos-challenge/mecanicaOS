using Oficina.Domain.Common.Interfaces;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;

namespace Oficina.UnitTests.Application;

internal static class UserTestData
{
    public const string StrongPassword = "Oficina@2026";
    public const string PasswordHash = "$2a$12$hash";

    public static User User(Role role = Role.Admin, string email = "ana@oficina.com") =>
        Oficina.Domain.Identity.Entities.User.Create("Ana Lima", Email.Create(email).Value, PasswordHash, role).Value;

    public static User Inactive(Role role = Role.Admin)
    {
        var user = User(role);
        user.Deactivate();
        return user;
    }

    public static User Deleted()
    {
        var user = User();
        ((ISoftDeletable)user).MarkDeleted(new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero));
        return user;
    }
}
