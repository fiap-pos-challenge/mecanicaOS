using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Oficina.FunctionalTests.Fixtures;
using Oficina.Infrastructure.Addresses;

namespace Oficina.FunctionalTests.Infrastructure;

public sealed class ViaCepAddressLookupTests
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static ViaCepAddressLookup Lookup(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new HttpClient(new StubHttpHandler(respond)) { BaseAddress = new Uri("https://viacep.test/") }, NullLogger<ViaCepAddressLookup>.Instance);

    [Fact]
    public async Task FindAsync_ServiceAnswersWithAddress_MapsTheFields()
    {
        var address = await Lookup(_ => StubHttpHandler.Json("""{"logradouro":"Praça da Sé","bairro":"Sé","localidade":"São Paulo","uf":"sp"}"""))
            .FindAsync("01001000", CancellationToken);

        Assert.NotNull(address);
        Assert.Equal(("Praça da Sé", "Sé", "São Paulo", "SP"), (address.Street, address.District, address.City, address.State));
        Assert.Null(address.Number);
    }

    [Fact]
    public async Task FindAsync_ServiceFailsOrDoesNotKnow_ReturnsNull()
    {
        Assert.Null(await Lookup(_ => StubHttpHandler.Json("""{"erro":true}""")).FindAsync("99999999", CancellationToken));
        Assert.Null(await Lookup(_ => StubHttpHandler.Json("{}", HttpStatusCode.BadGateway)).FindAsync("99999999", CancellationToken));
        Assert.Null(await Lookup(_ => StubHttpHandler.Json("not json")).FindAsync("99999999", CancellationToken));
        Assert.Null(await Lookup(_ => StubHttpHandler.Json("null")).FindAsync("99999999", CancellationToken));
    }
}
