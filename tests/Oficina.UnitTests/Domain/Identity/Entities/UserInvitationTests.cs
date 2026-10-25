using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Errors;

namespace Oficina.UnitTests.Domain.Identity.Entities;

public sealed class UserInvitationTests
{
    private const string Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    private const string PasswordHash = "$2a$12$hash";
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
    private static readonly Email InvitedEmail = Email.Create("novo@oficina.com").Value;

    [Fact]
    public void Invite_CreatesAnActiveUserWithoutPasswordWhoCannotSignIn()
    {
        var user = User.Invite(" Rafael Costa ", InvitedEmail, Role.Manager).Value;

        Assert.Equal("Rafael Costa", user.Name);
        Assert.Null(user.PasswordHash);
        Assert.True(user.HasPendingInvitation);
        Assert.Equal(UserStatus.InvitationPending, user.Status);
        Assert.False(user.CanLogin);
        Assert.Equal(Role.Manager, user.Role);
    }

    [Fact]
    public void Invite_InvalidName_Fails() =>
        Assert.Equal(IdentityErrors.InvalidName, User.Invite(" ", InvitedEmail, Role.Mechanic).Error);

    [Fact]
    public void DefinePassword_OnlyOnceAndWithAValidHash()
    {
        var user = User.Invite("Rafael", InvitedEmail, Role.Mechanic).Value;

        Assert.Equal(IdentityErrors.InvalidPasswordHash, user.DefinePassword(" ").Error);
        Assert.True(user.DefinePassword(PasswordHash).IsSuccess);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.True(user.CanLogin);
        Assert.Equal(IdentityErrors.InvitationAlreadyUsed, user.DefinePassword(PasswordHash).Error);
    }

    [Fact]
    public void Status_IsInactiveAfterDeactivation()
    {
        var user = User.Invite("Rafael", InvitedEmail, Role.Mechanic).Value;
        user.Deactivate();

        Assert.Equal(UserStatus.Inactive, user.Status);
    }

    [Fact]
    public void Issue_ExpiresIn72HoursAndIsOpen()
    {
        var userId = Guid.CreateVersion7();
        var createdBy = Guid.CreateVersion7();

        var invitation = UserInvitation.Issue(userId, Hash, createdBy, Now);

        Assert.Equal(userId, invitation.UserId);
        Assert.Equal(createdBy, invitation.CreatedBy);
        Assert.Equal(Hash, invitation.TokenHash);
        Assert.Equal(Now, invitation.CreatedAt);
        Assert.Equal(Now.AddHours(72), invitation.ExpiresAt);
        Assert.True(invitation.IsOpen);
        Assert.True(invitation.EnsureUsable(Now).IsSuccess);
    }

    [Fact]
    public void Accept_ConsumesTheInvitationOnce()
    {
        var invitation = UserInvitation.Issue(Guid.CreateVersion7(), Hash, null, Now);

        Assert.True(invitation.Accept(Now.AddHours(1)).IsSuccess);
        Assert.Equal(Now.AddHours(1), invitation.AcceptedAt);
        Assert.False(invitation.IsOpen);
        Assert.Equal(IdentityErrors.InvitationAlreadyUsed, invitation.Accept(Now.AddHours(2)).Error);
    }

    [Fact]
    public void Accept_AfterExpiry_Fails()
    {
        var invitation = UserInvitation.Issue(Guid.CreateVersion7(), Hash, null, Now);

        Assert.Equal(IdentityErrors.InvitationExpired, invitation.Accept(Now.AddHours(72)).Error);
        Assert.Null(invitation.AcceptedAt);
    }

    [Fact]
    public void Revoke_ClosesOnlyOpenInvitations()
    {
        var invitation = UserInvitation.Issue(Guid.CreateVersion7(), Hash, null, Now);
        invitation.Revoke(Now.AddMinutes(1));
        invitation.Revoke(Now.AddMinutes(2));

        Assert.Equal(Now.AddMinutes(1), invitation.RevokedAt);
        Assert.Equal(IdentityErrors.InvitationAlreadyUsed, invitation.EnsureUsable(Now).Error);
    }
}
