using Oficina.Application.Identity.Commands.AcceptInvitation;
using Oficina.Application.Identity.Queries.InspectInvitation;
using Oficina.Application.Users.Commands.InviteUser;
using Oficina.Application.Users.Commands.ResendInvitation;
using Oficina.Domain.Identity.Enums;

namespace Oficina.UnitTests.Application.Identity;

public sealed class InvitationValidatorTests
{
    [Fact]
    public void Invite_RequiresNameEmailAndAValidRole()
    {
        var validator = new InviteUserCommandValidator();

        Assert.True(validator.Validate(new InviteUserCommand("Rafael", "rafael@oficina.com", Role.Manager)).IsValid);
        Assert.False(validator.Validate(new InviteUserCommand("R", "rafael@oficina.com", Role.Mechanic)).IsValid);
        Assert.False(validator.Validate(new InviteUserCommand("Rafael", "", Role.Mechanic)).IsValid);
        Assert.False(validator.Validate(new InviteUserCommand("Rafael", "rafael@oficina.com", (Role)99)).IsValid);
    }

    [Fact]
    public void Resend_RequiresTheId()
    {
        var validator = new ResendInvitationCommandValidator();

        Assert.True(validator.Validate(new ResendInvitationCommand(Guid.CreateVersion7())).IsValid);
        Assert.False(validator.Validate(new ResendInvitationCommand(Guid.Empty)).IsValid);
    }

    [Fact]
    public void Accept_RequiresTokenAndStrongPassword()
    {
        var validator = new AcceptInvitationCommandValidator();

        Assert.True(validator.Validate(new AcceptInvitationCommand("token", UserTestData.StrongPassword)).IsValid);
        Assert.False(validator.Validate(new AcceptInvitationCommand("token", "fraca")).IsValid);
        Assert.False(validator.Validate(new AcceptInvitationCommand("", UserTestData.StrongPassword)).IsValid);
        Assert.False(validator.Validate(new AcceptInvitationCommand(new string('a', 129), UserTestData.StrongPassword)).IsValid);
    }

    [Fact]
    public void Inspect_RequiresTheToken()
    {
        var validator = new InspectInvitationQueryValidator();

        Assert.True(validator.Validate(new InspectInvitationQuery("token")).IsValid);
        Assert.False(validator.Validate(new InspectInvitationQuery(" ")).IsValid);
    }
}
