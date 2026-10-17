using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class PartsApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    private static object NewPart(string code = "pec-9001", decimal unitPrice = 150m, decimal cost = 90m, string? barcode = "7891234567895") => new
    {
        code,
        name = "Correia dentada",
        type = "Part",
        unitOfMeasure = "un",
        unitPrice,
        cost,
        minimumStock = 2m,
        supplier = "Auto Peças Centro",
        barcode,
    };

    [Fact]
    public async Task Crud_CreatesReadsListsUpdatesDeactivatesAndReactivates()
    {
        using var manager = await ClientAsync(Role.Manager);
        using var mechanic = await ClientAsync(Role.Mechanic);

        using var created = await manager.PostAsJsonAsync("/api/admin/parts", NewPart(), CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await ReadJsonAsync(created);
        var id = body.GetProperty("id").GetGuid();
        Assert.Equal("PEC-9001", body.GetProperty("code").GetString());
        Assert.Equal(90m, body.GetProperty("cost").GetDecimal());
        Assert.Equal(0m, body.GetProperty("stockQuantity").GetDecimal());
        Assert.EndsWith($"/api/admin/parts/{id}", created.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);

        var seenByMechanic = await ReadJsonAsync(await mechanic.GetAsync($"/api/admin/parts/{id}", CancellationToken));
        Assert.Equal(JsonValueKind.Null, seenByMechanic.GetProperty("cost").ValueKind);
        Assert.Equal("Auto Peças Centro", seenByMechanic.GetProperty("supplier").GetString());

        var byCode = await ReadJsonAsync(await mechanic.GetAsync("/api/admin/parts?term=pec-9001", CancellationToken));
        Assert.Equal(1, byCode.GetProperty("totalItems").GetInt32());
        var byName = await ReadJsonAsync(await mechanic.GetAsync("/api/admin/parts?term=Correia", CancellationToken));
        Assert.Equal(1, byName.GetProperty("totalItems").GetInt32());

        using var updated = await manager.PutAsJsonAsync($"/api/admin/parts/{id}", NewPart("PEC-9002", unitPrice: 170m, barcode: null), CancellationToken);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var updatedBody = await ReadJsonAsync(updated);
        Assert.Equal("PEC-9002", updatedBody.GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.Null, updatedBody.GetProperty("barcode").ValueKind);

        var managerHistory = (await ReadJsonAsync(await manager.GetAsync($"/api/admin/parts/{id}/price-history", CancellationToken)))
            .GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(3, managerHistory.Length);
        Assert.Equal("UnitPrice", managerHistory[0].GetProperty("field").GetString());
        Assert.Equal(170m, managerHistory[0].GetProperty("newValue").GetDecimal());
        Assert.Contains(managerHistory, change => change.GetProperty("field").GetString() == "Cost");
        var mechanicHistory = (await ReadJsonAsync(await mechanic.GetAsync($"/api/admin/parts/{id}/price-history", CancellationToken)))
            .GetProperty("items").EnumerateArray().ToArray();
        Assert.DoesNotContain(mechanicHistory, change => change.GetProperty("field").GetString() == "Cost");

        Assert.Equal(HttpStatusCode.NoContent, (await manager.PostAsync($"/api/admin/parts/{id}/deactivate", null, CancellationToken)).StatusCode);
        Assert.Equal(0, (await ReadJsonAsync(await mechanic.GetAsync("/api/admin/parts", CancellationToken))).GetProperty("totalItems").GetInt32());
        Assert.Equal(1, (await ReadJsonAsync(await mechanic.GetAsync("/api/admin/parts?status=Inactive", CancellationToken))).GetProperty("totalItems").GetInt32());
        Assert.Equal(1, (await ReadJsonAsync(await mechanic.GetAsync("/api/admin/parts?includeInactive=true", CancellationToken))).GetProperty("totalItems").GetInt32());
        await AssertProblemAsync(await manager.PostAsync($"/api/admin/parts/{id}/deactivate", null, CancellationToken), HttpStatusCode.Conflict, "inventory.alreadyInactive");

        Assert.Equal(HttpStatusCode.NoContent, (await manager.PostAsync($"/api/admin/parts/{id}/reactivate", null, CancellationToken)).StatusCode);
        await AssertProblemAsync(await manager.PostAsync($"/api/admin/parts/{id}/reactivate", null, CancellationToken), HttpStatusCode.Conflict, "inventory.alreadyActive");
    }

    [Fact]
    public async Task Create_DuplicatedCode_Returns409OnTheCodeField()
    {
        using var admin = await ClientAsync(Role.Admin);
        await admin.PostAsJsonAsync("/api/admin/parts", NewPart(), CancellationToken);

        using var duplicated = await admin.PostAsJsonAsync("/api/admin/parts", NewPart("PEC-9001"), CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, duplicated.StatusCode);
        var problem = await ReadJsonAsync(duplicated);
        Assert.Equal("inventory.duplicatePartCode", problem.GetProperty("code").GetString());
        Assert.Equal("code", Assert.Single(problem.GetProperty("errors").EnumerateArray()).GetProperty("field").GetString());
    }

    [Fact]
    public async Task Update_CodeOfAnotherPart_Returns409()
    {
        using var admin = await ClientAsync(Role.Admin);
        await admin.PostAsJsonAsync("/api/admin/parts", NewPart(), CancellationToken);
        using var second = await admin.PostAsJsonAsync("/api/admin/parts", NewPart("PEC-9100", barcode: null), CancellationToken);
        var secondId = (await ReadJsonAsync(second)).GetProperty("id").GetGuid();

        await AssertProblemAsync(
            await admin.PutAsJsonAsync($"/api/admin/parts/{secondId}", NewPart("PEC-9001"), CancellationToken),
            HttpStatusCode.Conflict,
            "inventory.duplicatePartCode");
    }

    [Fact]
    public async Task Create_InvalidFields_Returns400WithEveryField()
    {
        using var admin = await ClientAsync(Role.Admin);

        using var response = await admin.PostAsJsonAsync(
            "/api/admin/parts",
            new { code = "pe ça", name = "X", type = "Part", unitOfMeasure = "", unitPrice = -1m, cost = 1m, minimumStock = -1m, supplier = (string?)null, barcode = "123" },
            CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var codes = (await ReadJsonAsync(response)).GetProperty("errors").EnumerateArray().Select(error => error.GetProperty("code").GetString()).ToArray();
        Assert.Contains("inventory.invalidPartCode", codes);
        Assert.Contains("inventory.invalidName", codes);
        Assert.Contains("inventory.invalidUnitOfMeasure", codes);
        Assert.Contains("inventory.invalidMinimumStock", codes);
        Assert.Contains("inventory.invalidBarcode", codes);
    }

    [Fact]
    public async Task UnknownPart_Returns404()
    {
        using var admin = await ClientAsync(Role.Admin);
        var id = Guid.CreateVersion7();

        await AssertProblemAsync(await admin.GetAsync($"/api/admin/parts/{id}", CancellationToken), HttpStatusCode.NotFound, "inventory.partNotFound");
        await AssertProblemAsync(await admin.GetAsync($"/api/admin/parts/{id}/price-history", CancellationToken), HttpStatusCode.NotFound, "inventory.partNotFound");
        await AssertProblemAsync(await admin.PutAsJsonAsync($"/api/admin/parts/{id}", NewPart(), CancellationToken), HttpStatusCode.NotFound, "inventory.partNotFound");
        await AssertProblemAsync(await admin.PostAsync($"/api/admin/parts/{id}/deactivate", null, CancellationToken), HttpStatusCode.NotFound, "inventory.partNotFound");
        await AssertProblemAsync(await admin.PostAsync($"/api/admin/parts/{id}/reactivate", null, CancellationToken), HttpStatusCode.NotFound, "inventory.partNotFound");
    }
}
