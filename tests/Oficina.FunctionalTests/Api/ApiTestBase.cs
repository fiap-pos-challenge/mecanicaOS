using System.Net;
using System.Text.Json;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;
using Oficina.Infrastructure.Messaging.Outbox;
using Oficina.Infrastructure.Persistence;

namespace Oficina.FunctionalTests.Api;

public abstract class ApiTestBase(SqlServerFixture database) : IAsyncLifetime
{
    private FunctionalApiFactory? _factory;

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    protected SqlServerFixture Database => database;

    protected FunctionalApiFactory Factory => _factory ?? throw new InvalidOperationException(nameof(Factory));

    public virtual async ValueTask InitializeAsync()
    {
        await database.ResetAsync();
        _factory = new FunctionalApiFactory(database);
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    protected async Task<User> SaveUserAsync(Role role, string? email = null, string passwordHash = "$2a$12$hash")
    {
        var user = User.Create($"{role} User", Email.Create(email ?? $"{role.ToString().ToLowerInvariant()}.{Guid.CreateVersion7():N}@oficina.com").Value, passwordHash, role).Value;
        await using var context = database.CreateContext(TimeProvider.System);
        context.Users.Add(user);
        await CommitAsync(context);
        return user;
    }

    protected async Task<HttpClient> ClientAsync(Role role) => Factory.CreateClient(await SaveUserAsync(role));

    protected static async Task CommitAsync(OficinaDbContext context) =>
        Assert.True((await new UnitOfWork(context, new OutboxWriter()).CommitAsync(CancellationToken)).IsSuccess);

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken)).RootElement;

    protected static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, (await ReadJsonAsync(response)).GetProperty("code").GetString());
    }
}
