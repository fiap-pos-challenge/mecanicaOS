using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Abstractions.Security;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Errors;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class AuthApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    private const string Password = "Oficina@2026";

    [Fact]
    public async Task Login_ValidCredentials_ReturnsATokenThatOpensMe()
    {
        await SaveUserWithPasswordAsync(Role.Attendant, "ana@oficina.com");
        using var client = Factory.CreateClient();

        using var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "ANA@oficina.com", password = Password }, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = await ReadJsonAsync(login);
        Assert.True(token.GetProperty("expiresAt").GetDateTimeOffset() > DateTimeOffset.UnixEpoch);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.GetProperty("accessToken").GetString());
        var me = await ReadJsonAsync(await client.GetAsync("/api/auth/me", CancellationToken));
        Assert.Equal("ana@oficina.com", me.GetProperty("email").GetString());
        Assert.Equal("Attendant", me.GetProperty("role").GetString());
        Assert.Equal("Attendant User", me.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Login_WrongPasswordOrInactiveUser_Returns401()
    {
        var inactive = await SaveUserWithPasswordAsync(Role.Mechanic, "inativo@oficina.com");
        await SaveUserWithPasswordAsync(Role.Mechanic, "ativo@oficina.com");
        await Database.QueryAsync($"UPDATE users SET isActive = 0 WHERE id = '{inactive.Id}'; SELECT 1 AS done");
        using var client = Factory.CreateClient();

        await AssertProblemAsync(await client.PostAsJsonAsync("/api/auth/login", new { email = "ativo@oficina.com", password = "Errada@2026" }, CancellationToken), HttpStatusCode.Unauthorized, IdentityErrors.InvalidCredentials.Code);
        await AssertProblemAsync(await client.PostAsJsonAsync("/api/auth/login", new { email = "inativo@oficina.com", password = Password }, CancellationToken), HttpStatusCode.Unauthorized, IdentityErrors.InvalidCredentials.Code);
    }

    [Fact]
    public async Task Login_SixthAttemptWithinAMinute_Returns429()
    {
        using var client = Factory.CreateClient();
        var credentials = new { email = "x@oficina.com", password = "Errada@2026" };

        for (var attempt = 0; attempt < 5; attempt++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", credentials, CancellationToken)).StatusCode);
        }

        using var blocked = await client.PostAsJsonAsync("/api/auth/login", credentials, CancellationToken);

        await AssertProblemAsync(blocked, HttpStatusCode.TooManyRequests, CommonErrors.TooManyRequests.Code);
    }

    [Fact]
    public async Task Me_WithoutTokenOrAfterDeactivation_Returns401()
    {
        var user = await SaveUserAsync(Role.Mechanic);
        using var client = Factory.CreateClient(user);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me", CancellationToken)).StatusCode);
        await Database.QueryAsync($"UPDATE users SET isActive = 0 WHERE id = '{user.Id}'; SELECT 1 AS done");

        await AssertProblemAsync(await client.GetAsync("/api/auth/me", CancellationToken), HttpStatusCode.Unauthorized, CommonErrors.Unauthorized.Code);
        await AssertProblemAsync(await Factory.CreateClient().GetAsync("/api/auth/me", CancellationToken), HttpStatusCode.Unauthorized, CommonErrors.Unauthorized.Code);
    }

    private async Task<Oficina.Domain.Identity.Entities.User> SaveUserWithPasswordAsync(Role role, string email) =>
        await SaveUserAsync(role, email, Factory.Services.GetRequiredService<IPasswordHasher>().Hash(Password));
}
