using System.Net;
using Microsoft.AspNetCore.Hosting;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class HttpPipelineTests(SqlServerFixture database) : ApiTestBase(database)
{
    [Theory]
    [InlineData(FunctionalApiFactory.AllowedOrigin, true)]
    [InlineData("http://evil.example.com", false)]
    public async Task Cors_AllowsOnlyTheConfiguredOrigin(string origin, bool allowed)
    {
        using var client = Factory.CreateClient();
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        preflight.Headers.Add("Origin", origin);
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type");

        using var response = await client.SendAsync(preflight, CancellationToken);

        Assert.Equal(allowed, response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values) && values.Single() == origin);
    }

    [Fact]
    public async Task Swagger_IsNotPublishedOutsideDevelopment()
    {
        await using var production = Factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = production.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json", CancellationToken);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health", CancellationToken)).StatusCode);
    }
}
