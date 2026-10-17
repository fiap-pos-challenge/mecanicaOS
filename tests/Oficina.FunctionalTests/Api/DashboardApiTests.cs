using System.Net;
using System.Net.Http.Json;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class DashboardApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    private static async Task<(Guid OrderId, Guid PartId)> SeedAsync(HttpClient admin, SqlServerFixture database)
    {
        var customer = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/customers", new { name = "Maria Souza", document = "529.982.247-25", phone = "(11) 98765-4321", email = "maria@exemplo.com", messagingConsent = new { whatsApp = true, email = true } }, CancellationToken));
        var customerId = customer.GetProperty("id").GetGuid();
        var vehicle = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/vehicles", new { customerId, licensePlate = "BRA2E19", brand = "Fiat", model = "Uno", year = 2020, mileage = 50_000 }, CancellationToken));
        var part = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/parts", new { code = "PST-001", name = "Pastilha de freio", type = "Part", unitOfMeasure = "un", unitPrice = 80m, cost = 40m, minimumStock = 4m }, CancellationToken));
        var order = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/service-orders", new { customerId, vehicleId = vehicle.GetProperty("id").GetGuid(), problemDescription = "Freio chiando", promisedAt = TimeProvider.System.GetUtcNow().AddHours(1) }, CancellationToken));
        var orderId = order.GetProperty("id").GetGuid();
        await database.QueryAsync($"UPDATE serviceOrders SET promisedAt = DATEADD(hour, -5, SYSDATETIMEOFFSET()) WHERE id = '{orderId}'");
        return (orderId, part.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Snapshot_ShowsKpisYardAndAttention_AndActionsHideTheItem()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var attendant = await ClientAsync(Role.Attendant);
        var (orderId, partId) = await SeedAsync(admin, Database);

        var snapshot = await ReadJsonAsync(await attendant.GetAsync("/api/admin/dashboard?period=week", CancellationToken));
        Assert.Equal(4, snapshot.GetProperty("kpis").GetArrayLength());
        Assert.Equal(1, snapshot.GetProperty("kpis")[1].GetProperty("value").GetInt64());
        Assert.Equal(7, snapshot.GetProperty("dailyRevenue").GetArrayLength());
        Assert.Equal(1, snapshot.GetProperty("yard")[0].GetProperty("count").GetInt32());
        var attention = snapshot.GetProperty("attention").EnumerateArray().ToList();
        var stock = Assert.Single(attention, item => item.GetProperty("kind").GetString() == "LowStock");
        Assert.Equal($"stock-{partId:N}", stock.GetProperty("id").GetString());
        var late = Assert.Single(attention, item => item.GetProperty("kind").GetString() == "LateOrder");
        Assert.Equal(orderId.ToString(), late.GetProperty("target").GetProperty("id").GetString());
        Assert.Single((await ReadJsonAsync(await attendant.GetAsync("/api/admin/dashboard", CancellationToken))).GetProperty("dailyRevenue").EnumerateArray());
        await AssertProblemAsync(await attendant.GetAsync("/api/admin/dashboard?period=year", CancellationToken), HttpStatusCode.BadRequest, "dashboard.period");

        await AssertProblemAsync(await attendant.PostAsJsonAsync($"/api/admin/dashboard/attention/{stock.GetProperty("id").GetString()}/actions", new { action = "CreatePurchaseOrder" }, CancellationToken), HttpStatusCode.UnprocessableEntity, "dashboard.noSupplier");
        await admin.PostAsJsonAsync("/api/admin/inventory/suppliers", new { name = "Auto Peças Centro", document = (string?)null, email = "pedidos@autopecas.example.com", phone = (string?)null, leadTimeDays = 2 }, CancellationToken);
        await AssertProblemAsync(await attendant.PostAsJsonAsync($"/api/admin/dashboard/attention/{stock.GetProperty("id").GetString()}/actions", new { action = "SendReminder" }, CancellationToken), HttpStatusCode.UnprocessableEntity, "dashboard.actionNotAllowed");
        Assert.Equal(HttpStatusCode.NoContent, (await attendant.PostAsJsonAsync($"/api/admin/dashboard/attention/{stock.GetProperty("id").GetString()}/actions", new { action = "CreatePurchaseOrder" }, CancellationToken)).StatusCode);
        var orders = await ReadJsonAsync(await admin.GetAsync("/api/admin/inventory/purchase-orders", CancellationToken));
        Assert.Equal(8m, Assert.Single(orders.GetProperty("items").EnumerateArray()).GetProperty("lines")[0].GetProperty("quantity").GetDecimal());
        Assert.Equal(HttpStatusCode.NoContent, (await attendant.PostAsJsonAsync($"/api/admin/dashboard/attention/{late.GetProperty("id").GetString()}/actions", new { action = "SendNewDeadline" }, CancellationToken)).StatusCode);
        Assert.Contains(Factory.Emails.Messages, message => message.To == "maria@exemplo.com");

        var after = (await ReadJsonAsync(await attendant.GetAsync("/api/admin/dashboard?period=month", CancellationToken))).GetProperty("attention").EnumerateArray();
        Assert.DoesNotContain(after, item => item.GetProperty("kind").GetString() is "LowStock" or "LateOrder");
        await AssertProblemAsync(await attendant.PostAsJsonAsync($"/api/admin/dashboard/attention/{late.GetProperty("id").GetString()}/actions", new { action = "SendNewDeadline" }, CancellationToken), HttpStatusCode.NotFound, "dashboard.attentionNotFound");
    }

    [Fact]
    public async Task Layout_IsSavedPerUser()
    {
        using var attendant = await ClientAsync(Role.Attendant);
        using var manager = await ClientAsync(Role.Manager);

        Assert.Empty((await ReadJsonAsync(await attendant.GetAsync("/api/admin/dashboard/layout", CancellationToken))).GetProperty("cards").EnumerateArray());
        var cards = new[] { new { id = "attention", visible = true }, new { id = "kpis", visible = false } };
        var saved = await ReadJsonAsync(await attendant.PutAsJsonAsync("/api/admin/dashboard/layout", new { cards }, CancellationToken));
        Assert.Equal("attention", saved.GetProperty("cards")[0].GetProperty("id").GetString());
        Assert.False((await ReadJsonAsync(await attendant.GetAsync("/api/admin/dashboard/layout", CancellationToken))).GetProperty("cards")[1].GetProperty("visible").GetBoolean());
        Assert.Empty((await ReadJsonAsync(await manager.GetAsync("/api/admin/dashboard/layout", CancellationToken))).GetProperty("cards").EnumerateArray());
        await AssertProblemAsync(await attendant.PutAsJsonAsync("/api/admin/dashboard/layout", new { cards = new[] { new { id = "weather", visible = true } } }, CancellationToken), HttpStatusCode.BadRequest, "dashboard.layout");
    }
}
