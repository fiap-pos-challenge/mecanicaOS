using System.Net;
using System.Net.Http.Json;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;
using Oficina.Infrastructure.Repositories.ServiceOrders;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class ServicesApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    private static readonly object NewService = new
    {
        name = "Troca de óleo",
        description = "Inclui filtro",
        basePrice = 150.50m,
        estimatedMinutes = 45,
    };

    [Fact]
    public async Task Crud_CreatesReadsUpdatesListsDeactivatesAndReactivates()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var attendant = await ClientAsync(Role.Attendant);

        using var created = await admin.PostAsJsonAsync("/api/admin/services", NewService, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await ReadJsonAsync(created)).GetProperty("id").GetGuid();
        Assert.EndsWith($"/api/admin/services/{id}", created.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);

        var fetched = await ReadJsonAsync(await attendant.GetAsync($"/api/admin/services/{id}", CancellationToken));
        Assert.Equal("Troca de óleo", fetched.GetProperty("name").GetString());
        Assert.Equal(150.50m, fetched.GetProperty("basePrice").GetDecimal());
        Assert.Equal(45, fetched.GetProperty("estimatedMinutes").GetInt32());

        using var updated = await admin.PutAsJsonAsync(
            $"/api/admin/services/{id}",
            new { name = "Troca de óleo sintético", basePrice = 199.90m, estimatedMinutes = 50 },
            CancellationToken);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var updatedBody = await ReadJsonAsync(updated);
        Assert.Equal("Troca de óleo sintético", updatedBody.GetProperty("name").GetString());
        Assert.Equal(199.90m, updatedBody.GetProperty("basePrice").GetDecimal());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, updatedBody.GetProperty("description").ValueKind);

        var page = await ReadJsonAsync(await attendant.GetAsync("/api/admin/services?page=1&pageSize=10", CancellationToken));
        Assert.Equal(1, page.GetProperty("totalItems").GetInt32());

        using var deactivated = await admin.PostAsync($"/api/admin/services/{id}/deactivate", null, CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deactivated.StatusCode);
        var row = Assert.Single(await Database.QueryAsync($"SELECT isActive, deletedAt FROM services WHERE id = '{id}'"));
        Assert.Equal(false, row["isActive"]);
        Assert.Null(row["deletedAt"]);

        var inactive = await ReadJsonAsync(await attendant.GetAsync($"/api/admin/services/{id}", CancellationToken));
        Assert.False(inactive.GetProperty("isActive").GetBoolean());
        var activePage = await ReadJsonAsync(await attendant.GetAsync("/api/admin/services", CancellationToken));
        Assert.Equal(0, activePage.GetProperty("totalItems").GetInt32());
        var fullPage = await ReadJsonAsync(await attendant.GetAsync("/api/admin/services?includeInactive=true", CancellationToken));
        Assert.Equal(1, fullPage.GetProperty("totalItems").GetInt32());

        await AssertProblemAsync(
            await admin.PutAsJsonAsync($"/api/admin/services/{id}", NewService, CancellationToken),
            HttpStatusCode.Conflict,
            CatalogErrors.CannotChangeInactive.Code);

        using var reactivated = await admin.PostAsync($"/api/admin/services/{id}/reactivate", null, CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, reactivated.StatusCode);
        var reactivatedBody = await ReadJsonAsync(await attendant.GetAsync($"/api/admin/services/{id}", CancellationToken));
        Assert.True(reactivatedBody.GetProperty("isActive").GetBoolean());
        var pageAfterReactivation = await ReadJsonAsync(await attendant.GetAsync("/api/admin/services", CancellationToken));
        Assert.Equal(1, pageAfterReactivation.GetProperty("totalItems").GetInt32());
    }

    [Fact]
    public async Task Create_InvalidData_Returns400WithEveryInvalidField()
    {
        using var client = await ClientAsync(Role.Admin);

        using var response = await client.PostAsJsonAsync("/api/admin/services", new { name = "T", basePrice = 0m, estimatedMinutes = 0 }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors").EnumerateArray().ToArray();
        Assert.Equal(new[] { "name", "basePrice", "estimatedMinutes" }, errors.Select(error => error.GetProperty("field").GetString()).ToArray());
        Assert.Equal(CatalogErrors.InvalidBasePrice.Code, errors[1].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Update_UnknownService_Returns404()
    {
        using var client = await ClientAsync(Role.Admin);

        using var response = await client.PutAsJsonAsync($"/api/admin/services/{Guid.CreateVersion7()}", NewService, CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, CatalogErrors.NotFound.Code);
    }

    [Fact]
    public async Task Deactivate_AlreadyInactive_Returns409()
    {
        var service = TestData.Service();
        service.Deactivate();
        await using (var context = Database.CreateContext(TimeProvider.System))
        {
            context.Services.Add(service);
            await CommitAsync(context);
        }

        using var client = await ClientAsync(Role.Admin);

        await AssertProblemAsync(await client.PostAsync($"/api/admin/services/{service.Id}/deactivate", null, CancellationToken), HttpStatusCode.Conflict, CatalogErrors.AlreadyInactive.Code);
    }

    [Fact]
    public async Task Reactivate_AlreadyActive_Returns409()
    {
        var service = TestData.Service();
        await using (var context = Database.CreateContext(TimeProvider.System))
        {
            context.Services.Add(service);
            await CommitAsync(context);
        }

        using var client = await ClientAsync(Role.Admin);

        await AssertProblemAsync(await client.PostAsync($"/api/admin/services/{service.Id}/reactivate", null, CancellationToken), HttpStatusCode.Conflict, CatalogErrors.AlreadyActive.Code);
    }

    [Fact]
    public async Task Deactivate_ServiceUsedInServiceOrder_KeepsTheOrderItem()
    {
        var customer = TestData.Customer();
        var vehicle = TestData.Vehicle(customer.Id);
        var service = TestData.Service(120m);
        var order = TestData.ServiceOrder(customer.Id, vehicle.Id, 7001);
        order.StartDiagnosis(null, TestData.Today);
        order.AddServiceItem(service, 1);
        await using (var context = Database.CreateContext(TimeProvider.System))
        {
            context.Customers.Add(customer);
            context.Vehicles.Add(vehicle);
            context.Services.Add(service);
            new ServiceOrderRepository(context).Add(order);
            await CommitAsync(context);
        }

        using var client = await ClientAsync(Role.Admin);
        using var response = await client.PostAsync($"/api/admin/services/{service.Id}/deactivate", null, CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var reader = Database.CreateContext(TimeProvider.System);
        var loaded = await new ServiceOrderRepository(reader).GetByIdAsync(order.Id, CancellationToken);
        var item = Assert.Single(loaded!.ServiceItems);
        Assert.Equal(service.Id, item.ServiceId);
        Assert.Equal(120m, item.UnitPrice.Amount);
    }
}
