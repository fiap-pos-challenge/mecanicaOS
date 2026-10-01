using Oficina.Application.Common.Pagination;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;
using Oficina.Infrastructure.Messaging.Outbox;
using Oficina.Infrastructure.Persistence;
using Oficina.Infrastructure.Queries.Identity;

namespace Oficina.FunctionalTests.Queries;

[Collection(nameof(DatabaseCollection))]
public sealed class UserQueriesTests(SqlServerFixture database) : IAsyncLifetime
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await database.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task ListAsync_ReturnsThePageOrderedByNameWithoutTracking()
    {
        await SaveAsync(NewUser("Zeca", "zeca@oficina.com"), NewUser("Ana", "ana@oficina.com"));
        await using var context = database.CreateContext(TimeProvider.System);

        var page = await new UserQueries(context).ListAsync(new PageRequest(1, 1), CancellationToken);

        Assert.Equal("Ana", Assert.Single(page.Items).Name);
        Assert.Equal(2, page.TotalItems);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByIdAndEmail_ReturnTheUser()
    {
        var user = NewUser("Ana", "ana@oficina.com");
        await SaveAsync(user);
        await using var context = database.CreateContext(TimeProvider.System);
        var queries = new UserQueries(context);

        var byId = await queries.GetByIdAsync(user.Id, CancellationToken);
        var byEmail = await queries.GetByEmailAsync(Email.Create("ANA@oficina.com").Value, CancellationToken);

        Assert.Equal("ana@oficina.com", byId!.Email);
        Assert.Equal(Role.Attendant, byId.Role);
        Assert.Equal(user.Id, byEmail!.Id);
        Assert.Null(await queries.GetByIdAsync(Guid.CreateVersion7(), CancellationToken));
        Assert.Null(await queries.GetByEmailAsync(Email.Create("x@oficina.com").Value, CancellationToken));
    }

    private static User NewUser(string name, string email) =>
        User.Create(name, Email.Create(email).Value, "$2a$12$hash", Role.Attendant).Value;

    private async Task SaveAsync(params User[] users)
    {
        await using var context = database.CreateContext(TimeProvider.System);
        context.Users.AddRange(users);
        Assert.True((await new UnitOfWork(context, new OutboxWriter()).CommitAsync(CancellationToken)).IsSuccess);
    }
}
