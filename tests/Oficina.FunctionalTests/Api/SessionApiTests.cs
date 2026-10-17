using System.Net;
using System.Net.Http.Json;
using Oficina.Application.Identity.Sessions;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;
using Oficina.Infrastructure.Security;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class SessionApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    private const string Password = "Oficina@2026";

    private static readonly Lazy<string> PasswordHash = new(() => new BCryptPasswordHasher().Hash(Password));

    private async Task<(HttpClient Client, string Email)> SignedOutClientAsync()
    {
        var email = $"sessao.{Guid.CreateVersion7():N}@oficina.com";
        await SaveUserAsync(Role.Attendant, email, PasswordHash.Value);
        return (Factory.CreateClient(), email);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("accessToken").GetString()!;
    }

    [Fact]
    public async Task Login_SetsTheRefreshCookie_AndRefreshRotatesUntilLogout()
    {
        var (client, email) = await SignedOutClientAsync();

        using var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password }, CancellationToken);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"));
        Assert.Contains("oficina_refresh=", cookie, StringComparison.Ordinal);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.False((await ReadJsonAsync(login)).GetProperty("requiresTwoFactor").GetBoolean());

        using var refreshed = await client.PostAsync("/api/auth/refresh", null, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.Equal(3, (await ReadJsonAsync(refreshed)).GetProperty("accessToken").GetString()!.Split('.').Length);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null, CancellationToken)).StatusCode);
        await AssertProblemAsync(await client.PostAsync("/api/auth/refresh", null, CancellationToken), HttpStatusCode.Unauthorized, "identity.sessionExpired");
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null, CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_Returns401()
    {
        using var client = Factory.CreateClient();

        await AssertProblemAsync(await client.PostAsync("/api/auth/refresh", null, CancellationToken), HttpStatusCode.Unauthorized, "identity.sessionExpired");
    }

    [Fact]
    public async Task TwoFactor_SetupEnableAndSignInWithTheCode()
    {
        var (client, email) = await SignedOutClientAsync();
        client.DefaultRequestHeaders.Authorization = new("Bearer", await LoginAsync(client, email));

        await AssertProblemAsync(await client.PostAsJsonAsync("/api/auth/2fa/enable", new { code = "123456" }, CancellationToken), HttpStatusCode.Conflict, "identity.twoFactorNotSetUp");
        var setup = await ReadJsonAsync(await client.PostAsync("/api/auth/2fa/setup", null, CancellationToken));
        var secret = setup.GetProperty("secret").GetString()!;
        Assert.StartsWith("otpauth://totp/Oficina:", setup.GetProperty("otpAuthUri").GetString(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/2fa/enable", new { code = "000000" }, CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/2fa/enable", new { code = Totp.Code(secret, TimeProvider.System.GetUtcNow()) }, CancellationToken)).StatusCode);

        using var anonymous = Factory.CreateClient();
        using var login = await anonymous.PostAsJsonAsync("/api/auth/login", new { email, password = Password }, CancellationToken);
        var challenge = await ReadJsonAsync(login);
        Assert.True(challenge.GetProperty("requiresTwoFactor").GetBoolean());
        Assert.False(login.Headers.Contains("Set-Cookie"));
        var challengeId = challenge.GetProperty("challengeId").GetGuid();

        await AssertProblemAsync(await anonymous.PostAsJsonAsync("/api/auth/2fa/verify", new { challengeId, code = "000000" }, CancellationToken), HttpStatusCode.Unauthorized, "identity.invalidTwoFactorCode");
        using var verified = await anonymous.PostAsJsonAsync("/api/auth/2fa/verify", new { challengeId, code = Totp.Code(secret, TimeProvider.System.GetUtcNow()) }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
        Assert.Contains("oficina_refresh=", Assert.Single(verified.Headers.GetValues("Set-Cookie")), StringComparison.Ordinal);
        await AssertProblemAsync(await anonymous.PostAsJsonAsync("/api/auth/2fa/verify", new { challengeId, code = Totp.Code(secret, TimeProvider.System.GetUtcNow()) }, CancellationToken), HttpStatusCode.Unauthorized, "identity.invalidTwoFactorCode");

        var byCode = await ReadJsonAsync(await client.PostAsJsonAsync("/api/auth/reauthenticate", new { code = Totp.Code(secret, TimeProvider.System.GetUtcNow()) }, CancellationToken));
        using var disable = new HttpRequestMessage(HttpMethod.Post, "/api/auth/2fa/disable");
        disable.Headers.Add("X-Reauthentication", byCode.GetProperty("reauthToken").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(disable, CancellationToken)).StatusCode);
        var row = Assert.Single(await Database.QueryAsync($"SELECT twoFactorEnabled, twoFactorSecret FROM users WHERE email = '{email}'"));
        Assert.Equal(false, row["twoFactorEnabled"]);
        Assert.Null(row["twoFactorSecret"]);
    }

    [Fact]
    public async Task Reauthenticate_RightPasswordGivesASingleUseToken()
    {
        var (client, email) = await SignedOutClientAsync();
        client.DefaultRequestHeaders.Authorization = new("Bearer", await LoginAsync(client, email));

        await AssertProblemAsync(await client.PostAsJsonAsync("/api/auth/reauthenticate", new { password = "Errada@2026" }, CancellationToken), HttpStatusCode.UnprocessableEntity, "auth.reauthenticationFailed");
        await AssertProblemAsync(await client.PostAsJsonAsync("/api/auth/reauthenticate", new { code = "123456" }, CancellationToken), HttpStatusCode.UnprocessableEntity, "auth.reauthenticationFailed");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/reauthenticate", new { }, CancellationToken)).StatusCode);
        var token = (await ReadJsonAsync(await client.PostAsJsonAsync("/api/auth/reauthenticate", new { password = Password }, CancellationToken))).GetProperty("reauthToken").GetString();

        using var first = new HttpRequestMessage(HttpMethod.Post, "/api/auth/2fa/disable");
        first.Headers.Add("X-Reauthentication", token);
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(first, CancellationToken)).StatusCode);
        using var second = new HttpRequestMessage(HttpMethod.Post, "/api/auth/2fa/disable");
        second.Headers.Add("X-Reauthentication", token);
        await AssertProblemAsync(await client.SendAsync(second, CancellationToken), HttpStatusCode.Forbidden, "settings.reauthenticationRequired");
        await AssertProblemAsync(await client.PostAsync("/api/auth/2fa/disable", null, CancellationToken), HttpStatusCode.Forbidden, "settings.reauthenticationRequired");
    }
}
