using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Repositories;
using Oficina.Domain.Identity.Services;

namespace Oficina.UnitTests.Application.Users;

public abstract class UserHandlerFixture
{
    protected UserHandlerFixture()
    {
        Policy = new UserAdministrationPolicy(Users);
        CurrentUser.UserId.Returns(ActingAdmin.Id);
        Users.GetByIdAsync(Target.Id, Arg.Any<CancellationToken>()).Returns(Target);
        Users.CountActiveAdminsAsync(Arg.Any<CancellationToken>()).Returns(2);
        UnitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
    }

    protected IUserRepository Users { get; } = Substitute.For<IUserRepository>();

    protected ICurrentUser CurrentUser { get; } = Substitute.For<ICurrentUser>();

    protected IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();

    protected UserAdministrationPolicy Policy { get; }

    protected User ActingAdmin { get; } = UserTestData.User(email: "admin@oficina.com");

    protected User Target { get; } = UserTestData.User(email: "target@oficina.com");

    protected void ActAsTarget() => CurrentUser.UserId.Returns(Target.Id);

    protected void ActAnonymously() => CurrentUser.UserId.Returns((Guid?)null);

    protected Task AssertNotCommittedAsync() => UnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
}
