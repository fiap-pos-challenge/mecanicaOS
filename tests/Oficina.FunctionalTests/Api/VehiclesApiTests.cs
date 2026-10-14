using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
    public async Task Crud_CreatesReadsListsUpdatesAndSoftDeletes()
    {
        var admin = await ClientAsync(Role.Admin);
        var customerId = await CreateCustomerAsync(admin);

        var request = new CreateVehicleRequest("BRA2E19", "Fiat", "Uno", 2026, customerId);
        using var created = await admin.PostAsJsonAsync("/api/admin/vehicles", request, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var vehicleId = (await ReadJsonAsync(created)).GetProperty("id").GetGuid();
        Assert.EndsWith($"/api/admin/vehicles/{vehicleId}", created.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);

        var detail = await ReadJsonAsync(await admin.GetAsync($"/api/admin/vehicles/{vehicleId}", CancellationToken));
        Assert.Equal("BRA2E19", detail.GetProperty("licensePlate").GetString());
        Assert.Equal(customerId, detail.GetProperty("owner").GetProperty("id").GetGuid());
        Assert.Equal("Maria Souza", detail.GetProperty("owner").GetProperty("name").GetString());
        Assert.Empty(detail.GetProperty("serviceOrders").EnumerateArray());

        var page = await ReadJsonAsync(await admin.GetAsync("/api/admin/vehicles?page=1&pageSize=10", CancellationToken));
        Assert.Contains(page.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == vehicleId);

        var filtered = await ReadJsonAsync(await admin.GetAsync("/api/admin/vehicles?plate=BRA2E19", CancellationToken));
        Assert.Single(filtered.GetProperty("items").EnumerateArray());

        var noMatch = await admin.GetAsync("/api/admin/vehicles?plate=ZZZ9999", CancellationToken);
        Assert.Equal(HttpStatusCode.OK, noMatch.StatusCode);
        Assert.Empty((await ReadJsonAsync(noMatch)).GetProperty("items").EnumerateArray());

        using var updated = await admin.PutAsJsonAsync($"/api/admin/vehicles/{vehicleId}", new UpdateVehicleRequest("Hyundai", "HB20", 2025), CancellationToken);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var updatedBody = await ReadJsonAsync(updated);
        Assert.Equal("Hyundai", updatedBody.GetProperty("brand").GetString());
        Assert.Equal("HB20", updatedBody.GetProperty("model").GetString());
        Assert.Equal("BRA2E19", updatedBody.GetProperty("licensePlate").GetString());

        using var deleted = await admin.DeleteAsync($"/api/admin/vehicles/{vehicleId}", CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/admin/vehicles/{vehicleId}", CancellationToken)).StatusCode);
        var pageAfterDelete = await ReadJsonAsync(await admin.GetAsync("/api/admin/vehicles?page=1&pageSize=10", CancellationToken));
        Assert.DoesNotContain(pageAfterDelete.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == vehicleId);
    }

    [Theory]
    [InlineData("old format", "ABC1234")]
    [InlineData("mercosul format", "ABC1D23")]
    public async Task Create_AcceptedPlateFormats_Returns201(string scenario, string plate)
    {
        _ = scenario;
        var admin = await ClientAsync(Role.Admin);
        var customerId = await CreateCustomerAsync(admin);

        using var response = await admin.PostAsJsonAsync("/api/admin/vehicles", new CreateVehicleRequest(plate, "Fiat", "Uno", 2026, customerId), CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidPlate_Returns400()
    {
        var admin = await ClientAsync(Role.Admin);
        var customerId = await CreateCustomerAsync(admin);

        using var response = await admin.PostAsJsonAsync("/api/admin/vehicles", new CreateVehicleRequest("111-BBBB", "Fiat", "Uno", 2026, customerId), CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single((await ReadJsonAsync(response)).GetProperty("errors").EnumerateArray());
        Assert.Equal("vehicle.invalidLicensePlate", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Create_YearBeforeFirstAllowedYear_Returns400()
    {
        var admin = await ClientAsync(Role.Admin);
        var customerId = await CreateCustomerAsync(admin);

        using var response = await admin.PostAsJsonAsync("/api/admin/vehicles", new CreateVehicleRequest("BRA2E19", "Fiat", "Uno", 1899, customerId), CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_YearAfterNextYear_Returns400()
    {
        var admin = await ClientAsync(Role.Admin);
        var customerId = await CreateCustomerAsync(admin);
        var farFutureYear = TimeProvider.System.GetUtcNow().Year + 2;

        using var response = await admin.PostAsJsonAsync("/api/admin/vehicles", new CreateVehicleRequest("BRA2E19", "Fiat", "Uno", farFutureYear, customerId), CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_CustomerDoesNotExist_Returns404()
    {
        var admin = await ClientAsync(Role.Admin);

        await AssertProblemAsync(
            await admin.PostAsJsonAsync("/api/admin/vehicles", new CreateVehicleRequest("BRA2E19", "Fiat", "Uno", 2026, Guid.CreateVersion7()), CancellationToken),
            HttpStatusCode.NotFound,
            "customer.notFound");
    }

    [Fact]
    public async Task Create_DuplicatedPlate_Returns409()
    {
        var admin = await ClientAsync(Role.Admin);
        var customerId = await CreateCustomerAsync(admin);
        await admin.PostAsJsonAsync("/api/admin/vehicles", new CreateVehicleRequest("BRA2E19", "Fiat", "Uno", 2026, customerId), CancellationToken);

        await AssertProblemAsync(
            await admin.PostAsJsonAsync("/api/admin/vehicles", new CreateVehicleRequest("BRA2E19", "Hyundai", "HB20", 2025, customerId), CancellationToken),
            HttpStatusCode.Conflict,
            "vehicle.duplicatedLicensePlate");
    }

    [Fact]
    public async Task Create_PlateOfSoftDeletedVehicle_Returns409()
    {
        var admin = await ClientAsync(Role.Admin);
        var customerId = await CreateCustomerAsync(admin);
        using var firstCreated = await admin.PostAsJsonAsync("/api/admin/vehicles", new CreateVehicleRequest("BRA2E19", "Fiat", "Uno", 2026, customerId), CancellationToken);
        var firstId = (await ReadJsonAsync(firstCreated)).GetProperty("id").GetGuid();
        await admin.DeleteAsync($"/api/admin/vehicles/{firstId}", CancellationToken);

        await AssertProblemAsync(
            await admin.PostAsJsonAsync("/api/admin/vehicles", new CreateVehicleRequest("BRA2E19", "Hyundai", "HB20", 2025, customerId), CancellationToken),
            HttpStatusCode.Conflict,
            "vehicle.duplicatedLicensePlate");
    }

    [Fact]
    public async Task Create_SameCustomerTwoVehicles_BothReturn201()
    {
        var admin = await ClientAsync(Role.Admin);
        var customerId = await CreateCustomerAsync(admin);

        using var first = await admin.PostAsJsonAsync("/api/admin/vehicles", new CreateVehicleRequest("BRA2E19", "Fiat", "Uno", 2026, customerId), CancellationToken);
        using var second = await admin.PostAsJsonAsync("/api/admin/vehicles", new CreateVehicleRequest("ABC1234", "Hyundai", "HB20", 2025, customerId), CancellationToken);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task Update_UnknownVehicle_Returns404()
    {
        var admin = await ClientAsync(Role.Admin);

        await AssertProblemAsync(
            await admin.PutAsJsonAsync($"/api/admin/vehicles/{Guid.CreateVersion7()}", new UpdateVehicleRequest("Hyundai", "HB20", 2025), CancellationToken),
            HttpStatusCode.NotFound,
            "vehicle.notFound");
    }

    [Fact]
    public async Task GetById_UnknownVehicle_Returns404()
    {
        var admin = await ClientAsync(Role.Admin);

        await AssertProblemAsync(
            await admin.GetAsync($"/api/admin/vehicles/{Guid.CreateVersion7()}", CancellationToken),
            HttpStatusCode.NotFound,
            "vehicle.notFound");
    }

    [Fact]
    public async Task Delete_VehicleWithOpenServiceOrder_Returns409WithoutChanges()
    {
        var admin = await ClientAsync(Role.Admin);
        var customerId = await CreateCustomerAsync(admin);
        using var created = await admin.PostAsJsonAsync("/api/admin/vehicles", new CreateVehicleRequest("BRA2E19", "Fiat", "Uno", 2026, customerId), CancellationToken);
        var vehicleId = (await ReadJsonAsync(created)).GetProperty("id").GetGuid();

        await using (var context = Database.CreateContext(TimeProvider.System))
        {
            context.ServiceOrders.Add(TestData.ServiceOrder(customerId, vehicleId, 1));
            await CommitAsync(context);
        }

        await AssertProblemAsync(
            await admin.DeleteAsync($"/api/admin/vehicles/{vehicleId}", CancellationToken),
            HttpStatusCode.Conflict,
            "vehicle.hasOpenServiceOrder");
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/admin/vehicles/{vehicleId}", CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task GetById_WithServiceOrders_ReturnsHistoryFromNewestToOldest()
    {
        var admin = await ClientAsync(Role.Admin);
        var customerId = await CreateCustomerAsync(admin);
        using var created = await admin.PostAsJsonAsync("/api/admin/vehicles", new CreateVehicleRequest("BRA2E19", "Fiat", "Uno", 2026, customerId), CancellationToken);
        var vehicleId = (await ReadJsonAsync(created)).GetProperty("id").GetGuid();

        await using (var context = Database.CreateContext(TimeProvider.System))
        {
            var service = TestData.Service();
            var deliveredOrder = TestData.ServiceOrder(customerId, vehicleId, 1);
            deliveredOrder.StartDiagnosis(null, TestData.Today.AddMinutes(10));
            deliveredOrder.AddServiceItem(service, 1);
            deliveredOrder.SendQuote(TestData.Token(), null, TestData.Today.AddMinutes(30));
            deliveredOrder.ApproveQuote(null, TestData.Today.AddHours(1));
            deliveredOrder.Complete(null, TestData.Today.AddHours(3));
            deliveredOrder.Deliver(null, TestData.Today.AddHours(4));

            context.Services.Add(service);
            context.ServiceOrders.Add(deliveredOrder);
            context.ServiceOrders.Add(TestData.ServiceOrder(customerId, vehicleId, 2));
            await CommitAsync(context);
        }

        var history = (await ReadJsonAsync(await admin.GetAsync($"/api/admin/vehicles/{vehicleId}", CancellationToken)))
            .GetProperty("serviceOrders").EnumerateArray().ToArray();

        Assert.Equal(2, history.Length);
        Assert.Equal(2, history[0].GetProperty("number").GetInt64());
        Assert.Equal("Received", history[0].GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, history[0].GetProperty("completedAt").ValueKind);
        Assert.Equal(1, history[1].GetProperty("number").GetInt64());
        Assert.Equal("Delivered", history[1].GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, history[1].GetProperty("completedAt").ValueKind);
    }

    [Fact]
    public async Task Delete_UnknownVehicle_Returns404()
    {
        var admin = await ClientAsync(Role.Admin);

        await AssertProblemAsync(
            await admin.DeleteAsync($"/api/admin/vehicles/{Guid.CreateVersion7()}", CancellationToken),
            HttpStatusCode.NotFound,
            "vehicle.notFound");
    }

    private async Task<Guid> CreateCustomerAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/admin/customers", NewCustomer, CancellationToken);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }
}
