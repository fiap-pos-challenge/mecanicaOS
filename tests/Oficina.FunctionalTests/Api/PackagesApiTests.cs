using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class PackagesApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    [Fact]
    public async Task Crud_CalculatesSavingsRecordsPriceAndBlocksDeactivatingItsItems()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var attendant = await ClientAsync(Role.Attendant);
        var serviceId = await CreateServiceAsync(admin, 100m);
        var partId = await CreatePartAsync(admin, 25.5m);

        using var created = await admin.PostAsJsonAsync("/api/admin/packages", Package(serviceId, partId, 140m), CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await ReadJsonAsync(created);
        var id = body.GetProperty("id").GetGuid();
        Assert.Equal(151m, body.GetProperty("itemsTotal").GetDecimal());
        Assert.Equal(11m, body.GetProperty("savings").GetDecimal());
        Assert.Equal(10000, body.GetProperty("suggestion").GetProperty("minMileage").GetInt32());
        var items = body.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(2, items.Length);
        Assert.Contains(items, item => item.GetProperty("kind").GetString() == "Part" && item.GetProperty("name").GetString() == "Filtro de ar");

        var fetched = await ReadJsonAsync(await attendant.GetAsync($"/api/admin/packages/{id}", CancellationToken));
        Assert.Equal("Revisão 10 mil", fetched.GetProperty("name").GetString());
        Assert.Equal(1, (await ReadJsonAsync(await attendant.GetAsync("/api/admin/packages?term=10 mil", CancellationToken))).GetProperty("totalItems").GetInt32());

        using var updated = await admin.PutAsJsonAsync($"/api/admin/packages/{id}", Package(serviceId, partId, 130m) with { Suggestion = null }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await ReadJsonAsync(updated)).GetProperty("suggestion").ValueKind);
        var history = (await ReadJsonAsync(await attendant.GetAsync($"/api/admin/packages/{id}/price-history", CancellationToken)))
            .GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(2, history.Length);
        Assert.Equal("PackagePrice", history[0].GetProperty("field").GetString());

        await AssertProblemAsync(await admin.PostAsync($"/api/admin/parts/{partId}/deactivate", null, CancellationToken), HttpStatusCode.Conflict, "catalog.usedByActivePackage");
        using var blockedService = await admin.PostAsync($"/api/admin/services/{serviceId}/deactivate", null, CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, blockedService.StatusCode);
        Assert.Contains("Revisão 10 mil", (await ReadJsonAsync(blockedService)).GetProperty("detail").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/admin/packages/{id}/deactivate", null, CancellationToken)).StatusCode);
        await AssertProblemAsync(await admin.PutAsJsonAsync($"/api/admin/packages/{id}", Package(serviceId, partId, 130m), CancellationToken), HttpStatusCode.Conflict, "catalog.packageCannotChangeInactive");
        await AssertProblemAsync(await admin.PostAsync($"/api/admin/packages/{id}/deactivate", null, CancellationToken), HttpStatusCode.Conflict, "catalog.packageAlreadyInactive");
        Assert.Equal(1, (await ReadJsonAsync(await attendant.GetAsync("/api/admin/packages?status=Inactive", CancellationToken))).GetProperty("totalItems").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/admin/parts/{partId}/deactivate", null, CancellationToken)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/admin/packages/{id}/reactivate", null, CancellationToken)).StatusCode);
        await AssertProblemAsync(await admin.PostAsync($"/api/admin/packages/{id}/reactivate", null, CancellationToken), HttpStatusCode.Conflict, "catalog.packageAlreadyActive");
    }

    [Fact]
    public async Task Create_UnknownOrInactiveItems_Returns400()
    {
        using var admin = await ClientAsync(Role.Admin);
        var serviceId = await CreateServiceAsync(admin, 100m);
        var partId = await CreatePartAsync(admin, 20m);
        await admin.PostAsync($"/api/admin/parts/{partId}/deactivate", null, CancellationToken);

        await AssertProblemAsync(
            await admin.PostAsJsonAsync("/api/admin/packages", Package(serviceId, partId, 100m), CancellationToken),
            HttpStatusCode.BadRequest,
            "catalog.invalidItems");
        await AssertProblemAsync(
            await admin.PostAsJsonAsync("/api/admin/packages", Package(serviceId, Guid.CreateVersion7(), 100m), CancellationToken),
            HttpStatusCode.BadRequest,
            "catalog.invalidItems");
    }

    [Fact]
    public async Task Create_InvalidFields_Returns400WithEveryField()
    {
        using var admin = await ClientAsync(Role.Admin);

        using var response = await admin.PostAsJsonAsync(
            "/api/admin/packages",
            new PackageBody("X", null, [], 0m, new SuggestionBody(5000, 1000)),
            CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var codes = (await ReadJsonAsync(response)).GetProperty("errors").EnumerateArray().Select(error => error.GetProperty("code").GetString()).ToArray();
        Assert.Contains("catalog.invalidPackageName", codes);
        Assert.Contains("catalog.invalidPackagePrice", codes);
        Assert.Contains("catalog.invalidItems", codes);
        Assert.Contains("catalog.invalidMileageRange", codes);
    }

    [Fact]
    public async Task UnknownPackage_Returns404()
    {
        using var admin = await ClientAsync(Role.Admin);
        var id = Guid.CreateVersion7();
        var serviceId = await CreateServiceAsync(admin, 100m);
        var partId = await CreatePartAsync(admin, 20m);

        await AssertProblemAsync(await admin.GetAsync($"/api/admin/packages/{id}", CancellationToken), HttpStatusCode.NotFound, "catalog.packageNotFound");
        await AssertProblemAsync(await admin.GetAsync($"/api/admin/packages/{id}/price-history", CancellationToken), HttpStatusCode.NotFound, "catalog.packageNotFound");
        await AssertProblemAsync(await admin.PutAsJsonAsync($"/api/admin/packages/{id}", Package(serviceId, partId, 50m), CancellationToken), HttpStatusCode.NotFound, "catalog.packageNotFound");
        await AssertProblemAsync(await admin.PostAsync($"/api/admin/packages/{id}/deactivate", null, CancellationToken), HttpStatusCode.NotFound, "catalog.packageNotFound");
        await AssertProblemAsync(await admin.PostAsync($"/api/admin/packages/{id}/reactivate", null, CancellationToken), HttpStatusCode.NotFound, "catalog.packageNotFound");
    }

    [Fact]
    public async Task Service_WithUsualParts_KeepsAndReplacesTheParts()
    {
        using var admin = await ClientAsync(Role.Admin);
        var partId = await CreatePartAsync(admin, 20m);

        using var created = await admin.PostAsJsonAsync(
            "/api/admin/services",
            new { name = "Troca de óleo", basePrice = 80m, estimatedMinutes = 30, commissionPercent = 10m, warrantyDays = 90, usualParts = new[] { new { partId, quantity = 1.5m } } },
            CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await ReadJsonAsync(created)).GetProperty("id").GetGuid();
        var usual = Assert.Single((await ReadJsonAsync(await admin.GetAsync($"/api/admin/services/{id}", CancellationToken))).GetProperty("usualParts").EnumerateArray());
        Assert.Equal(partId, usual.GetProperty("partId").GetGuid());
        Assert.Equal(1.5m, usual.GetProperty("quantity").GetDecimal());

        using var cleared = await admin.PutAsJsonAsync(
            $"/api/admin/services/{id}",
            new { name = "Troca de óleo", basePrice = 80m, estimatedMinutes = 30, commissionPercent = 10m, warrantyDays = 90, usualParts = Array.Empty<object>() },
            CancellationToken);
        Assert.Empty((await ReadJsonAsync(cleared)).GetProperty("usualParts").EnumerateArray());

        await AssertProblemAsync(
            await admin.PostAsJsonAsync(
                "/api/admin/services",
                new { name = "Alinhamento", basePrice = 80m, estimatedMinutes = 30, commissionPercent = 10m, warrantyDays = 90, usualParts = new[] { new { partId = Guid.CreateVersion7(), quantity = 1m } } },
                CancellationToken),
            HttpStatusCode.BadRequest,
            "catalog.invalidUsualParts");
    }

    private static PackageBody Package(Guid serviceId, Guid partId, decimal price) => new(
        "Revisão 10 mil",
        "Óleo e filtro",
        [new ItemBody("Service", serviceId, 1m), new ItemBody("Part", partId, 2m)],
        price,
        new SuggestionBody(10000, 15000));

    private async Task<Guid> CreateServiceAsync(HttpClient client, decimal price)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/admin/services",
            new { name = $"Serviço {Guid.CreateVersion7():N}"[..20], basePrice = price, estimatedMinutes = 30, commissionPercent = 10m, warrantyDays = 90 },
            CancellationToken);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreatePartAsync(HttpClient client, decimal price)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/admin/parts",
            new { code = $"FLT-{Random.Shared.Next(1000, 9999)}", name = "Filtro de ar", type = "Part", unitOfMeasure = "un", unitPrice = price, cost = 10m, minimumStock = 1m },
            CancellationToken);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }

    private sealed record ItemBody(string Kind, Guid ItemId, decimal Quantity);

    private sealed record SuggestionBody(int? MinMileage, int? MaxMileage);

    private sealed record PackageBody(string Name, string? Description, IReadOnlyList<ItemBody> Items, decimal Price, SuggestionBody? Suggestion);
}
