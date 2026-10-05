using System.Net;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Health;

[Collection(nameof(DatabaseCollection))]
public sealed class HealthEndpointTests(SqlServerFixture database)
{
    [Fact]
    public async Task Health_WithReachableDatabase_ReturnsHealthy()
    {
        await using var factory = new FunctionalApiFactory(database);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body);
    }
}
