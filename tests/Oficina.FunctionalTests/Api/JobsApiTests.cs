using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class JobsApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    private static async Task<Guid> CustomerAsync(HttpClient client)
    {
        var customer = await ReadJsonAsync(await client.PostAsJsonAsync("/api/admin/customers", new { name = "Maria Souza", document = "529.982.247-25", phone = "(11) 98765-4321", email = "maria@exemplo.com" }, CancellationToken));
        var id = customer.GetProperty("id").GetGuid();
        await client.PostAsJsonAsync("/api/admin/vehicles", new { customerId = id, licensePlate = "BRA2E19", brand = "Fiat", model = "Uno", year = 2020, mileage = 50_000 }, CancellationToken);
        return id;
    }

    [Fact]
    public async Task Insights_SummarizeCustomersAndHistory()
    {
        using var attendant = await ClientAsync(Role.Attendant);
        var customerId = await CustomerAsync(attendant);
        var vehicleId = (await ReadJsonAsync(await attendant.GetAsync($"/api/admin/customers/{customerId}/vehicles", CancellationToken))).GetProperty("items")[0].GetProperty("id").GetGuid();
        var unknown = Guid.CreateVersion7();

        var empty = await ReadJsonAsync(await attendant.PostAsJsonAsync("/api/admin/customers/summaries", new { customerIds = new[] { customerId, unknown } }, CancellationToken));
        var first = empty.GetProperty("items")[0];
        Assert.Equal(1, first.GetProperty("vehicleCount").GetInt32());
        Assert.Equal("NoReturn", first.GetProperty("situation").GetString());
        Assert.Equal(unknown, empty.GetProperty("items")[1].GetProperty("customerId").GetGuid());

        await attendant.PostAsJsonAsync("/api/admin/service-orders", new { customerId, vehicleId, problemDescription = "Revisão geral" }, CancellationToken);
        var open = (await ReadJsonAsync(await attendant.PostAsJsonAsync("/api/admin/customers/summaries", new { customerIds = new[] { customerId } }, CancellationToken))).GetProperty("items")[0];
        Assert.Equal("OpenServiceOrder", open.GetProperty("situation").GetString());
        Assert.Equal(1, open.GetProperty("openServiceOrders").GetInt32());
        var history = await ReadJsonAsync(await attendant.GetAsync($"/api/admin/customers/{customerId}/service-history", CancellationToken));
        var entry = Assert.Single(history.GetProperty("items").EnumerateArray());
        Assert.Equal("InProgress", entry.GetProperty("status").GetString());
        Assert.Equal("Revisão geral", entry.GetProperty("description").GetString());

        await AssertProblemAsync(await attendant.GetAsync($"/api/admin/customers/{unknown}/service-history", CancellationToken), HttpStatusCode.NotFound, "customer.notFound");
        await AssertProblemAsync(await attendant.PostAsJsonAsync("/api/admin/customers/summaries", new { customerIds = Enumerable.Range(0, 501).Select(_ => Guid.CreateVersion7()) }, CancellationToken), HttpStatusCode.BadRequest, "insights.tooManyCustomers");
    }

    [Fact]
    public async Task Export_IsForAdminOrManager_AndReturnsTheCustomerData()
    {
        using var manager = await ClientAsync(Role.Manager);
        using var attendant = await ClientAsync(Role.Attendant);
        var customerId = await CustomerAsync(manager);

        Assert.Equal(HttpStatusCode.Forbidden, (await attendant.PostAsync($"/api/admin/customers/{customerId}/data-export", null, CancellationToken)).StatusCode);
        await AssertProblemAsync(await manager.PostAsync($"/api/admin/customers/{Guid.CreateVersion7()}/data-export", null, CancellationToken), HttpStatusCode.NotFound, "customer.notFound");
        using var started = await manager.PostAsync($"/api/admin/customers/{customerId}/data-export", null, CancellationToken);
        Assert.Equal(HttpStatusCode.Accepted, started.StatusCode);
        var jobId = (await ReadJsonAsync(started)).GetProperty("id").GetGuid();

        var job = await ReadJsonAsync(await manager.GetAsync($"/api/admin/jobs/{jobId}", CancellationToken));
        Assert.Equal("Completed", job.GetProperty("status").GetString());
        Assert.EndsWith(".json", job.GetProperty("result").GetProperty("fileName").GetString(), StringComparison.Ordinal);
        var content = JsonDocument.Parse(job.GetProperty("result").GetProperty("content").GetString()!).RootElement;
        Assert.Equal("52998224725", content.GetProperty("customer").GetProperty("document").GetString());
        Assert.Equal("BRA2E19", content.GetProperty("vehicles")[0].GetProperty("licensePlate").GetString());
        await AssertProblemAsync(await attendant.GetAsync($"/api/admin/jobs/{jobId}", CancellationToken), HttpStatusCode.NotFound, "job.notFound");
        await AssertProblemAsync(await manager.GetAsync($"/api/admin/jobs/{Guid.CreateVersion7()}", CancellationToken), HttpStatusCode.NotFound, "job.notFound");
    }

    [Fact]
    public async Task Import_CreatesValidCustomersAndReportsErrorsByLine()
    {
        using var attendant = await ClientAsync(Role.Attendant);
        await CustomerAsync(attendant);
        const string Csv = "﻿nome;documento;telefone;email\r\nJoão Lima;111.444.777-35;11912345678;joao@exemplo.com\r\nX;123;1;errado\r\nMaria Dup;529.982.247-25;11987654321;\r\nAna Costa;11.222.333/0001-81;1133334444;\r\n";

        using var started = await attendant.PostAsJsonAsync("/api/admin/imports/customers", new { fileName = "clientes.csv", csv = Csv }, CancellationToken);
        Assert.Equal(HttpStatusCode.Accepted, started.StatusCode);
        var job = await ReadJsonAsync(await attendant.GetAsync($"/api/admin/jobs/{(await ReadJsonAsync(started)).GetProperty("id").GetGuid()}", CancellationToken));
        var result = job.GetProperty("result");
        Assert.Equal(4, result.GetProperty("totalLines").GetInt32());
        Assert.Equal(2, result.GetProperty("validLines").GetInt32());
        var errors = result.GetProperty("errors").EnumerateArray().ToList();
        Assert.Equal(4, errors.Count(error => error.GetProperty("line").GetInt32() == 3));
        Assert.Contains(errors, error => error.GetProperty("line").GetInt32() == 4 && error.GetProperty("field").GetString() == "documento");
        var customers = await ReadJsonAsync(await attendant.GetAsync("/api/admin/customers?page=1&pageSize=10", CancellationToken));
        Assert.Equal(3, customers.GetProperty("totalItems").GetInt32());

        var header = await ReadJsonAsync(await attendant.PostAsJsonAsync("/api/admin/imports/customers", new { fileName = "x.csv", csv = "nome,cpf\nA,B" }, CancellationToken));
        Assert.Equal(1, header.GetProperty("result").GetProperty("errors")[0].GetProperty("line").GetInt32());
        await AssertProblemAsync(await attendant.PostAsJsonAsync("/api/admin/imports/customers", new { fileName = "clientes.txt", csv = Csv }, CancellationToken), HttpStatusCode.BadRequest, "import.invalidFile");
        await AssertProblemAsync(await attendant.PostAsJsonAsync("/api/admin/imports/customers", new { fileName = "grande.csv", csv = new string('a', (2 * 1024 * 1024) + 1) }, CancellationToken), HttpStatusCode.RequestEntityTooLarge, "import.fileTooLarge");
    }
}
