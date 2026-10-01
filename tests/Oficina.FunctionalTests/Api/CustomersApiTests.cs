using System.Net;
using System.Net.Http.Json;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class CustomersApiTests(SqlServerFixture database) : ApiTestBase(database)
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
    public async Task Crud_CreatesReadsUpdatesSearchesListsAndSoftDeletes()
    {
        using var attendant = await ClientAsync(Role.Attendant);
        using var admin = await ClientAsync(Role.Admin);

        using var created = await attendant.PostAsJsonAsync("/api/admin/customers", NewCustomer, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await ReadJsonAsync(created)).GetProperty("id").GetGuid();
        Assert.EndsWith($"/api/admin/customers/{id}", created.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);

        var fetched = await ReadJsonAsync(await attendant.GetAsync($"/api/admin/customers/{id}", CancellationToken));
        Assert.Equal("52998224725", fetched.GetProperty("document").GetString());
        Assert.Equal("Cpf", fetched.GetProperty("documentType").GetString());

        using var updated = await attendant.PutAsJsonAsync($"/api/admin/customers/{id}", new { name = "Maria Oliveira", phone = "1133334444" }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var updatedBody = await ReadJsonAsync(updated);
        Assert.Equal("Maria Oliveira", updatedBody.GetProperty("name").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, updatedBody.GetProperty("address").ValueKind);

        using var found = await attendant.PostAsJsonAsync("/api/admin/customers/search", new { document = "52998224725" }, CancellationToken);
        Assert.Equal(id, (await ReadJsonAsync(found)).GetProperty("id").GetGuid());

        var page = await ReadJsonAsync(await attendant.GetAsync("/api/admin/customers?page=1&pageSize=10", CancellationToken));
        Assert.Equal(1, page.GetProperty("totalItems").GetInt32());

        using var deleted = await admin.DeleteAsync($"/api/admin/customers/{id}", CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        var row = Assert.Single(await Database.QueryAsync($"SELECT deletedAt FROM customers WHERE id = '{id}'"));
        Assert.NotNull(row["deletedAt"]);
        await AssertProblemAsync(await attendant.GetAsync($"/api/admin/customers/{id}", CancellationToken), HttpStatusCode.NotFound, CustomerErrors.NotFound.Code);
    }

    [Fact]
    public async Task Create_DuplicateDocument_Returns409()
    {
        using var client = await ClientAsync(Role.Admin);
        await client.PostAsJsonAsync("/api/admin/customers", NewCustomer, CancellationToken);

        using var response = await client.PostAsJsonAsync("/api/admin/customers", NewCustomer, CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, CustomerErrors.DuplicateDocument.Code);
    }

    [Fact]
    public async Task Create_InvalidData_Returns400WithEveryInvalidField()
    {
        using var client = await ClientAsync(Role.Admin);

        using var response = await client.PostAsJsonAsync("/api/admin/customers", new { name = "M", document = "123", phone = "1" }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var fields = (await ReadJsonAsync(response)).GetProperty("errors").EnumerateArray().Select(error => error.GetProperty("field").GetString()).ToArray();
        Assert.Equal(new[] { "name", "document", "phone" }, fields);
    }

    [Fact]
    public async Task Delete_CustomerWithVehicle_Returns409()
    {
        var customer = TestData.Customer();
        await using (var context = Database.CreateContext(TimeProvider.System))
        {
            context.Customers.Add(customer);
            context.Vehicles.Add(TestData.Vehicle(customer.Id));
            await CommitAsync(context);
        }

        using var client = await ClientAsync(Role.Admin);

        await AssertProblemAsync(await client.DeleteAsync($"/api/admin/customers/{customer.Id}", CancellationToken), HttpStatusCode.Conflict, CustomerErrors.HasActiveVehicles.Code);
    }
}
