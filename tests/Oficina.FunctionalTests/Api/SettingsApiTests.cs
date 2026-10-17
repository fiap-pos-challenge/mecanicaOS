using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;
using Oficina.Infrastructure.Security;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class SettingsApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    private const string Password = "Oficina@2026";

    private static readonly Lazy<string> PasswordHash = new(() => new BCryptPasswordHasher().Hash(Password));

    private static readonly object[] Hours =
    [
        new { weekday = "Monday", closed = false, opens = "08:00", closes = "18:00" },
        new { weekday = "Tuesday", closed = false, opens = "08:00", closes = "18:00" },
        new { weekday = "Wednesday", closed = false, opens = "08:00", closes = "18:00" },
        new { weekday = "Thursday", closed = false, opens = "08:00", closes = "18:00" },
        new { weekday = "Friday", closed = false, opens = "08:00", closes = "18:00" },
        new { weekday = "Saturday", closed = false, opens = "08:00", closes = "12:00" },
        new { weekday = "Sunday", closed = true, opens = "00:00", closes = "00:00" },
    ];

    private static readonly object Address = new { zipCode = "01310-100", street = "Avenida Paulista", number = "1000", district = "Bela Vista", city = "São Paulo", state = "sp" };

    private async Task<HttpClient> SignedInAdminAsync()
    {
        var email = $"admin.{Guid.CreateVersion7():N}@oficina.com";
        await SaveUserAsync(Role.Admin, email, PasswordHash.Value);
        var client = Factory.CreateClient();
        using var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await ReadJsonAsync(login)).GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<string> ReauthAsync(HttpClient client) =>
        (await ReadJsonAsync(await client.PostAsJsonAsync("/api/auth/reauthenticate", new { password = Password }, CancellationToken))).GetProperty("reauthToken").GetString()!;

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string url, object? body, string? reauth)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (reauth is not null)
        {
            request.Headers.Add("X-Reauthentication", reauth);
        }

        return await client.SendAsync(request, CancellationToken);
    }

    private static async Task<JsonElement> SensitiveAsync(HttpClient client, HttpMethod method, string url, object body)
    {
        using var response = await SendAsync(client, method, url, body, await ReauthAsync(client));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync(CancellationToken));
        return await ReadJsonAsync(response);
    }

    [Fact]
    public async Task Settings_ReadDefaults_SaveWithReauthentication_AndAudit()
    {
        using var admin = await SignedInAdminAsync();
        using var manager = await ClientAsync(Role.Manager);

        var workshop = await ReadJsonAsync(await manager.GetAsync("/api/admin/settings/workshop", CancellationToken));
        Assert.Equal(7, workshop.GetProperty("hours").GetArrayLength());
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync("/api/admin/settings/security", CancellationToken)).StatusCode);
        var workshopBody = new { name = "Oficina Exemplo", document = "11.222.333/0001-81", address = Address, hours = Hours };
        await AssertProblemAsync(await SendAsync(admin, HttpMethod.Put, "/api/admin/settings/workshop", workshopBody, null), HttpStatusCode.Forbidden, "settings.reauthenticationRequired");
        await AssertProblemAsync(await SendAsync(admin, HttpMethod.Put, "/api/admin/settings/workshop", workshopBody with { document = "11111111111111" }, "x"), HttpStatusCode.BadRequest, "settings.document");
        await AssertProblemAsync(await SendAsync(admin, HttpMethod.Put, "/api/admin/settings/workshop", workshopBody with { name = "Ab" }, "x"), HttpStatusCode.BadRequest, "settings.name");
        await AssertProblemAsync(await SendAsync(admin, HttpMethod.Put, "/api/admin/settings/workshop", workshopBody with { hours = Hours[..3] }, "x"), HttpStatusCode.BadRequest, "settings.hours");
        var saved = await SensitiveAsync(admin, HttpMethod.Put, "/api/admin/settings/workshop", workshopBody);
        Assert.Equal("11222333000181", saved.GetProperty("document").GetString());
        Assert.Equal("SP", saved.GetProperty("address").GetProperty("state").GetString());
        Assert.Equal(2, saved.GetProperty("plan").GetProperty("users").GetInt32());

        await AssertProblemAsync(await SendAsync(admin, HttpMethod.Post, "/api/admin/settings/workshop/logo", new { fileName = "logo.gif", contentType = "image/gif", dataUrl = "data:image/gif;base64,R0" }, "x"), HttpStatusCode.BadRequest, "settings.logo");
        var logo = await SensitiveAsync(admin, HttpMethod.Post, "/api/admin/settings/workshop/logo", new { fileName = "logo.png", contentType = "image/png", dataUrl = "data:image/png;base64,iVBORw0KGgo=" });
        Assert.Equal("data:image/png;base64,iVBORw0KGgo=", logo.GetProperty("logoUrl").GetString());

        var discounts = await ReadJsonAsync(await manager.GetAsync("/api/admin/settings/discounts", CancellationToken));
        Assert.Equal(4, discounts.GetProperty("limits").GetArrayLength());
        var rules = new[] { new { id = Guid.Empty, name = "Cliente fiel", type = "Percent", value = 5m, condition = "LoyalCustomer", isActive = true } };
        await AssertProblemAsync(await SendAsync(admin, HttpMethod.Put, "/api/admin/settings/discounts", new { rules, limits = new[] { new { role = "Attendant", percent = 150m } } }, "x"), HttpStatusCode.BadRequest, "settings.limits");
        var savedDiscounts = await SensitiveAsync(admin, HttpMethod.Put, "/api/admin/settings/discounts", new { rules, limits = new[] { new { role = "Attendant", percent = 12m } } });
        Assert.NotEqual(Guid.Empty, savedDiscounts.GetProperty("rules")[0].GetProperty("id").GetGuid());
        Assert.Contains(savedDiscounts.GetProperty("limits").EnumerateArray(), limit => limit.GetProperty("role").GetString() == "Attendant" && limit.GetProperty("percent").GetDecimal() == 12m);

        var payments = await ReadJsonAsync(await manager.GetAsync("/api/admin/settings/payments", CancellationToken));
        Assert.Contains(payments.GetProperty("methods").EnumerateArray(), method => method.GetString() == "Pix");
        var paymentBody = new { methods = new[] { "Pix", "Credit" }, maxInstallments = 10, interestFreeInstallments = 3, monthlyInterestPercent = 1.5m, fees = new[] { new { method = "Credit", feePercent = 3.49m, settlementDays = 30 } }, pixKey = "pix@oficina.com" };
        await AssertProblemAsync(await SendAsync(admin, HttpMethod.Put, "/api/admin/settings/payments", paymentBody with { methods = Array.Empty<string>() }, "x"), HttpStatusCode.BadRequest, "settings.methods");
        await AssertProblemAsync(await SendAsync(admin, HttpMethod.Put, "/api/admin/settings/payments", paymentBody with { interestFreeInstallments = 12 }, "x"), HttpStatusCode.BadRequest, "settings.installments");
        Assert.Equal("pix@oficina.com", (await SensitiveAsync(admin, HttpMethod.Put, "/api/admin/settings/payments", paymentBody)).GetProperty("pixKey").GetString());

        var messages = await ReadJsonAsync(await manager.GetAsync("/api/admin/settings/messages", CancellationToken));
        Assert.NotEmpty(messages.GetProperty("templates").EnumerateArray());
        var window = new { from = "08:00", to = "19:00", weekdays = new[] { "Saturday", "Monday" } };
        await AssertProblemAsync(await SendAsync(admin, HttpMethod.Put, "/api/admin/settings/messages", new { templates = new[] { new { kind = "QuoteSent", channel = "WhatsApp", body = "Olá {nome}" } }, window }, "x"), HttpStatusCode.BadRequest, "settings.templates");
        await AssertProblemAsync(await SendAsync(admin, HttpMethod.Put, "/api/admin/settings/messages", new { templates = Array.Empty<object>(), window = window with { from = "20:00" } }, "x"), HttpStatusCode.BadRequest, "settings.window");
        var savedMessages = await SensitiveAsync(admin, HttpMethod.Put, "/api/admin/settings/messages", new { templates = new[] { new { kind = "QuoteSent", channel = "WhatsApp", body = " Olá {cliente}, veja {link} " } }, window });
        Assert.Equal("Olá {cliente}, veja {link}", savedMessages.GetProperty("templates")[0].GetProperty("body").GetString());
        Assert.Equal("Monday", savedMessages.GetProperty("window").GetProperty("weekdays")[0].GetString());

        var security = await ReadJsonAsync(await admin.GetAsync("/api/admin/settings/security", CancellationToken));
        Assert.Equal(30, security.GetProperty("idleMinutes").GetInt32());
        await AssertProblemAsync(await SendAsync(admin, HttpMethod.Put, "/api/admin/settings/security", new { requireTwoFactorForAll = true, idleMinutes = 2, lockoutAttempts = 5, lockoutMinutes = 15 }, "x"), HttpStatusCode.BadRequest, "settings.idleMinutes");
        Assert.True((await SensitiveAsync(admin, HttpMethod.Put, "/api/admin/settings/security", new { requireTwoFactorForAll = true, idleMinutes = 20, lockoutAttempts = 5, lockoutMinutes = 15 })).GetProperty("requireTwoFactorForAll").GetBoolean());

        var fiscal = await ReadJsonAsync(await manager.GetAsync("/api/admin/settings/fiscal", CancellationToken));
        Assert.Equal(3, fiscal.GetProperty("integrations").GetArrayLength());

        var audit = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/settings/audit/search", new { area = "all", page = 1, pageSize = 20 }, CancellationToken));
        Assert.Equal(6, audit.GetProperty("totalItems").GetInt32());
        var paymentsAudit = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/settings/audit/search", new { area = "payments", actor = "admin", from = TimeProvider.System.GetUtcNow().ToString("yyyy-MM-dd"), page = 1, pageSize = 20 }, CancellationToken));
        Assert.Equal("Formas e taxas de pagamento alteradas", Assert.Single(paymentsAudit.GetProperty("items").EnumerateArray()).GetProperty("action").GetString());
        Assert.Equal(0, (await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/settings/audit/search", new { area = "stock", page = 1, pageSize = 20 }, CancellationToken))).GetProperty("totalItems").GetInt32());
        await AssertProblemAsync(await admin.PostAsJsonAsync("/api/admin/settings/audit/search", new { area = "nada", page = 1, pageSize = 20 }, CancellationToken), HttpStatusCode.BadRequest, "settings.area");
    }

    [Fact]
    public async Task Sessions_ListAndEndOtherSessions()
    {
        using var admin = await SignedInAdminAsync();
        using var other = await SignedInAdminAsync();

        var sessions = (await ReadJsonAsync(await admin.GetAsync("/api/admin/settings/sessions", CancellationToken))).GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(2, sessions.Count);
        var current = Assert.Single(sessions, session => session.GetProperty("current").GetBoolean());
        var target = Assert.Single(sessions, session => !session.GetProperty("current").GetBoolean());

        await AssertProblemAsync(await SendAsync(admin, HttpMethod.Delete, $"/api/admin/settings/sessions/{current.GetProperty("id").GetGuid()}", null, "x"), HttpStatusCode.NotFound, "settings.sessionNotFound");
        await AssertProblemAsync(await SendAsync(admin, HttpMethod.Delete, $"/api/admin/settings/sessions/{target.GetProperty("id").GetGuid()}", null, null), HttpStatusCode.Forbidden, "settings.reauthenticationRequired");
        using var ended = await SendAsync(admin, HttpMethod.Delete, $"/api/admin/settings/sessions/{target.GetProperty("id").GetGuid()}", null, await ReauthAsync(admin));
        Assert.Equal(HttpStatusCode.NoContent, ended.StatusCode);
        Assert.Single((await ReadJsonAsync(await admin.GetAsync("/api/admin/settings/sessions", CancellationToken))).GetProperty("items").EnumerateArray());
        await AssertProblemAsync(await SendAsync(admin, HttpMethod.Delete, $"/api/admin/settings/sessions/{target.GetProperty("id").GetGuid()}", null, "x"), HttpStatusCode.NotFound, "settings.sessionNotFound");
        var audit = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/settings/audit/search", new { area = "security", page = 1, pageSize = 20 }, CancellationToken));
        Assert.Equal("Sessão encerrada", audit.GetProperty("items")[0].GetProperty("action").GetString());
    }
}
