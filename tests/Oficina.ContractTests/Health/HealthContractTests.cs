using System.Net;
using Oficina.ContractTests.Fixtures;

namespace Oficina.ContractTests.Health;

public sealed class HealthContractTests(ContractApiFactory factory) : IClassFixture<ContractApiFactory>
{
    [Fact]
    public async Task Health_IsAnonymous_AndReturnsPlainTextStatus()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
    }
}
