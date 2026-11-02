using System.Net;
using System.Net.Http.Json;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class DevelopmentAdminSeederTests(SqlServerFixture database) : ApiTestBase(database)
{
    [Fact]
    public async Task Startup_InDevelopment_CreatesTheTestAdminOnceAndItCanLogIn()
    {
        await StartWithSeedAsync();
        await StartWithSeedAsync();

        var admin = Assert.Single(await Database.QueryAsync("SELECT email, role FROM users WHERE role = 'Admin'"));
        Assert.Equal("admin@oficina.local", admin["email"]);
        Assert.Equal("Admin", admin["role"]);
    }

    [Fact]
    public async Task Startup_InDevelopment_SeedsSampleDataOnce()
    {
        await StartWithSeedAsync();
        await StartWithSeedAsync();

        Assert.Equal(3, await CountAsync("customers"));
        Assert.Equal(3, await CountAsync("vehicles"));
        Assert.Equal(4, await CountAsync("services"));
        Assert.Equal(5, await CountAsync("parts"));
        Assert.Equal(5, await CountAsync("stockMovements"));
        Assert.Equal(3, await CountAsync("users"));
    }

    private async Task<int> CountAsync(string table) =>
        Convert.ToInt32((await Database.QueryAsync($"SELECT COUNT(*) AS total FROM {table}")).Single()["total"]);

    private async Task StartWithSeedAsync()
    {
        await using var factory = Factory.WithWebHostBuilder(builder => builder.UseSetting("DevelopmentAdmin:Email", "admin@oficina.local"));
        using var client = factory.CreateClient();

        using var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@oficina.local", password = "Admin@123" }, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }
}
