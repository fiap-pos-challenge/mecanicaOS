using System.Net;
using System.Net.Http.Json;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class FinanceApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    private async Task<(Guid Delivered, Guid Completed)> SeedAsync(HttpClient admin, HttpClient mechanic)
    {
        var customer = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/customers", new { name = "Maria Souza", document = "529.982.247-25", phone = "(11) 98765-4321" }, CancellationToken));
        var customerId = customer.GetProperty("id").GetGuid();
        var service = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/services", new { name = "Alinhamento 3D", basePrice = 120m, estimatedMinutes = 40, commissionPercent = 10m, warrantyDays = 90 }, CancellationToken));
        var ids = new List<Guid>();
        foreach (var plate in new[] { "BRA2E19", "ABC1D23" })
        {
            var vehicle = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/vehicles", new { customerId, licensePlate = plate, brand = "Fiat", model = "Uno", year = 2020, mileage = 50_000 }, CancellationToken));
            var order = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/service-orders", new { customerId, vehicleId = vehicle.GetProperty("id").GetGuid(), problemDescription = "Revisão" }, CancellationToken));
            var orderId = order.GetProperty("id").GetGuid();
            await mechanic.PostAsync($"/api/admin/service-orders/{orderId}/start-service", null, CancellationToken);
            await mechanic.PostAsJsonAsync($"/api/admin/service-orders/{orderId}/send-to-quote", new { description = "Alinhar", items = new object[] { new { kind = "Service", itemId = service.GetProperty("id").GetGuid() } } }, CancellationToken);
            ids.Add(orderId);
        }

        await Database.QueryAsync($"UPDATE serviceOrders SET status = 'Delivered', deliveredAt = SYSDATETIMEOFFSET(), totalAmount = 120 WHERE id = '{ids[0]}'");
        await Database.QueryAsync($"UPDATE serviceOrders SET status = 'Completed', completedAt = DATEADD(day, -3, SYSDATETIMEOFFSET()), totalAmount = 120 WHERE id = '{ids[1]}'");
        return (ids[0], ids[1]);
    }

    [Fact]
    public async Task Finance_SummarizesReceiptsReceivablesAndCommissions()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var mechanic = await ClientAsync(Role.Mechanic);
        using var manager = await ClientAsync(Role.Manager);
        using var attendant = await ClientAsync(Role.Attendant);
        var (delivered, completed) = await SeedAsync(admin, mechanic);

        var summary = await ReadJsonAsync(await manager.GetAsync("/api/admin/finance?period=week", CancellationToken));
        var kpis = summary.GetProperty("kpis");
        Assert.Equal(12_000, kpis.GetProperty("received").GetInt64());
        Assert.Equal(12_000, kpis.GetProperty("receivable").GetInt64());
        Assert.Equal(1, kpis.GetProperty("receivableCount").GetInt32());
        Assert.Equal(1_200, kpis.GetProperty("commissions").GetInt64());
        Assert.Equal("Balcão", summary.GetProperty("byMethod")[0].GetProperty("method").GetString());
        Assert.Equal(delivered, summary.GetProperty("receipts")[0].GetProperty("serviceOrderId").GetGuid());
        var receivable = Assert.Single(summary.GetProperty("receivables").EnumerateArray());
        Assert.Equal(completed, receivable.GetProperty("serviceOrderId").GetGuid());
        Assert.Equal(3, receivable.GetProperty("daysWaiting").GetInt32());
        Assert.Equal("Mechanic User", summary.GetProperty("commissions")[0].GetProperty("mechanicName").GetString());
        Assert.Equal("month", (await ReadJsonAsync(await manager.GetAsync("/api/admin/finance", CancellationToken))).GetProperty("period").GetString());
        await AssertProblemAsync(await manager.GetAsync("/api/admin/finance?period=year", CancellationToken), HttpStatusCode.BadRequest, "finance.period");
        Assert.Equal(HttpStatusCode.Forbidden, (await attendant.GetAsync("/api/admin/finance", CancellationToken)).StatusCode);

        var platform = await ReadJsonAsync(await admin.GetAsync("/api/admin/platform", CancellationToken));
        Assert.Equal(4, platform.GetProperty("plan").GetProperty("users").GetInt32());
        Assert.Equal(1, platform.GetProperty("usage").GetProperty("customers").GetInt32());
        Assert.Equal(2, platform.GetProperty("usage").GetProperty("serviceOrdersThisMonth").GetInt32());
        Assert.Contains(platform.GetProperty("modules").EnumerateArray(), module => module.GetProperty("id").GetString() == "fiscal" && !module.GetProperty("enabled").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync("/api/admin/platform", CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Documents_ListFiltersByKindAndCode()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var attendant = await ClientAsync(Role.Attendant);
        await using (var context = Database.CreateContext(TimeProvider.System))
        {
            var customer = TestData.Customer();
            var vehicle = TestData.Vehicle(customer.Id);
            var order = TestData.ServiceOrder(customer.Id, vehicle.Id, 7001);
            context.AddRange(customer, vehicle, order);
            foreach (var (code, kind) in new[] { ("OS-7001-AAAAAA", "ServiceOrder"), ("ORC-7001-BBBBBB", "Quote"), ("REC-7001-CCCCCC", "Receipt") })
            {
                context.IssuedDocuments.Add(Oficina.Domain.Documents.Entities.IssuedDocument.Issue(code, kind, order.Id, TimeProvider.System.GetUtcNow(),
                    new Oficina.Domain.Documents.Entities.DocumentSnapshot(7001, null, "Oficina", "Maria Souza", "BRA2E19 · Fiat Uno", [], 0, 0, 0, 150m, null, ["Pix"])));
            }

            await CommitAsync(context);
        }

        var all = await ReadJsonAsync(await attendant.GetAsync("/api/admin/documents", CancellationToken));
        Assert.Equal(3, all.GetProperty("totalItems").GetInt32());
        var receipts = await ReadJsonAsync(await attendant.GetAsync("/api/admin/documents?kind=Receipt", CancellationToken));
        Assert.Equal("REC-7001-CCCCCC", Assert.Single(receipts.GetProperty("items").EnumerateArray()).GetProperty("code").GetString());
        var byCode = await ReadJsonAsync(await attendant.GetAsync("/api/admin/documents?code=bbb&pageSize=500", CancellationToken));
        var quote = Assert.Single(byCode.GetProperty("items").EnumerateArray());
        Assert.Equal(150m, quote.GetProperty("total").GetDecimal());
        Assert.Equal("Maria Souza", quote.GetProperty("customerName").GetString());
        await AssertProblemAsync(await attendant.GetAsync("/api/admin/documents?kind=Nota", CancellationToken), HttpStatusCode.BadRequest, "documents.kind");
    }
}
