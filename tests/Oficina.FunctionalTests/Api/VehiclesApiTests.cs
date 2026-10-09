using System.Net;
using System.Net.Http.Json;
using Oficina.Api.Contracts.Vehicles;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class VehiclesApiTests(SqlServerFixture database) : ApiTestBase(database)
{

    private static readonly object NewCustomer = new
    {
        name = "Maria Souza",
        document = "529.982.247-25",
        email = "maria@exemplo.com",
        phone = "(11) 98765-4321",
        address = new { zipCode = "01310-100", street = "Av. Paulista", number = "1000", district = "Bela Vista", city = "São Paulo", state = "SP" },
    };

    [Fact]
    public async Task Handle_VehicleCreationWithValidArguments_Returns201Created()
    {
        var admin = await ClientAsync(Role.Admin);
        using var customerCreated = await admin.PostAsJsonAsync("/api/admin/customers", NewCustomer, CancellationToken);
        var id = (await ReadJsonAsync(customerCreated)).GetProperty("id").GetGuid();

        var request = new CreateVehicleRequest("AAA9999","Fiat", "Uno", 2026, id);
        var vehicleCreated = await admin.PostAsJsonAsync("/api/admin/vehicles", request, CancellationToken);

        Assert.Equal(HttpStatusCode.Created, vehicleCreated.StatusCode);

    }

}
