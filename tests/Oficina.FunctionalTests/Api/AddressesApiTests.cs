using System.Net;
using System.Net.Http.Json;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class AddressesApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    [Fact]
    public async Task Lookup_KnownZipCode_ReturnsTheAddress()
    {
        using var attendant = await ClientAsync(Role.Attendant);

        using var response = await attendant.PostAsJsonAsync("/api/addresses/lookup", new { zipCode = "01310-100" }, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("01310100", body.GetProperty("zipCode").GetString());
        Assert.Equal("Avenida Paulista", body.GetProperty("street").GetString());
        Assert.Equal("SP", body.GetProperty("state").GetString());
        Assert.Contains(Factory.ZipCodes.Requests, uri => uri.AbsolutePath == "/ws/01310100/json/");
    }

    [Fact]
    public async Task Lookup_UnknownZipCode_Returns404()
    {
        using var attendant = await ClientAsync(Role.Attendant);

        await AssertProblemAsync(
            await attendant.PostAsJsonAsync("/api/addresses/lookup", new { zipCode = "99999999" }, CancellationToken),
            HttpStatusCode.NotFound,
            "address.zipCodeNotFound");
    }

    [Fact]
    public async Task Lookup_InvalidZipCode_Returns400()
    {
        using var attendant = await ClientAsync(Role.Attendant);

        using var response = await attendant.PostAsJsonAsync("/api/addresses/lookup", new { zipCode = "123" }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("customer.invalidZipCode", Assert.Single((await ReadJsonAsync(response)).GetProperty("errors").EnumerateArray()).GetProperty("code").GetString());
    }
}
