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

        var admin = Assert.Single(await Database.QueryAsync("SELECT email, role FROM users"));
        Assert.Equal("admin@oficina.local", admin["email"]);
        Assert.Equal("Admin", admin["role"]);
    }

    private const string AdminPassword = "Seeder#Test2026";

    [Fact]
    public async Task Startup_WithoutTheAdminPassword_DoesNotCreateAnyUser()
    {
        await using var factory = Factory.WithWebHostBuilder(builder => builder.UseSetting("DevelopmentAdmin:Email", "admin@oficina.local"));
        using var client = factory.CreateClient();

        using var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@oficina.local", password = AdminPassword }, CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Empty(await Database.QueryAsync("SELECT email FROM users"));
    }

    private async Task StartWithSeedAsync()
    {
        await using var factory = Factory.WithWebHostBuilder(builder => builder
            .UseSetting("DevelopmentAdmin:Email", "admin@oficina.local")
            .UseSetting("DevelopmentAdmin:Password", AdminPassword));
        using var client = factory.CreateClient();

        using var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@oficina.local", password = AdminPassword }, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }
}
