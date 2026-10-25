using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Invitations;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.UnitTests.Application.Identity;

public abstract class InvitationFixture
{
    protected const string Token = "token-de-convite";
    protected static readonly DateTimeOffset Now = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    protected InvitationFixture()
    {
        Issuer = new InvitationIssuer(Invitations, EmailSender, Clock);
        Users.GetByIdAsync(Invited.Id, Arg.Any<CancellationToken>()).Returns(Invited);
        Invitations.ListOpenAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<UserInvitation>());
        UnitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        CurrentUser.UserId.Returns(AdminId);
    }

    protected Guid AdminId { get; } = Guid.CreateVersion7();

    protected IUserRepository Users { get; } = Substitute.For<IUserRepository>();

    protected IUserInvitationRepository Invitations { get; } = Substitute.For<IUserInvitationRepository>();

    protected IEmailSender EmailSender { get; } = Substitute.For<IEmailSender>();

    protected ICurrentUser CurrentUser { get; } = Substitute.For<ICurrentUser>();

    protected IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();

    protected FakeTimeProvider Clock { get; } = new(Now);

    protected InvitationIssuer Issuer { get; }

    protected User Invited { get; } = User.Invite("Rafael Costa", Email.Create("rafael@oficina.com").Value, Role.Mechanic).Value;

    protected UserInvitation Store(UserInvitation invitation)
    {
        Invitations.GetByTokenHashAsync(invitation.TokenHash, Arg.Any<CancellationToken>()).Returns(invitation);
        return invitation;
    }

    protected UserInvitation OpenInvitation() => Store(UserInvitation.Issue(Invited.Id, InvitationToken.Hash(Token), AdminId, Now.AddHours(-1)));
}
