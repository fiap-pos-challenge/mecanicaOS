using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Abstractions.Security;
using Oficina.Domain.Identity.Entities;

namespace Oficina.FunctionalTests.Fixtures;

public sealed class FunctionalApiFactory(SqlServerFixture database) : WebApplicationFactory<Program>
{
    public const string AllowedOrigin = "http://localhost:4200";

    public HttpClient CreateClient(User user)
    {
        var token = Services.GetRequiredService<ITokenGenerator>().Generate(user);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Oficina", database.ConnectionString);
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Outbox:PollingIntervalSeconds", "1");
        builder.UseSetting("Jwt:Secret", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("DevelopmentAdmin:Email", string.Empty);
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);
    }
}
