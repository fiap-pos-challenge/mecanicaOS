using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Automations;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class OperationsApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    private static object Rule(string name, string trigger, object[] conditions, object[] actions, object? wait = null) =>
        new { definition = new { name, trigger, conditions, wait, actions } };

    private async Task RunAutomationsAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AutomationService>().RunAsync(CancellationToken);
    }

    private async Task WaitForAsync(string sql)
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            if ((await Database.QueryAsync(sql)).Count > 0)
            {
                return;
            }

            await Task.Delay(250, CancellationToken);
        }

        Assert.Fail(sql);
    }

    [Fact]
    public async Task Automations_ValidateRunOnEventsAndRetryFailures()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var manager = await ClientAsync(Role.Manager);
        using var mechanic = await ClientAsync(Role.Mechanic);

        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/automations/rules", Rule("AB", "QuoteSent", [], [new { kind = "NotifyTeam", message = "Oi" }]), CancellationToken), HttpStatusCode.UnprocessableEntity, "automations.invalid");
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/automations/rules", Rule("Pedido errado", "QuoteSent", [], [new { kind = "CreatePurchaseOrder", message = (string?)null }]), CancellationToken), HttpStatusCode.UnprocessableEntity, "automations.invalid");
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/automations/rules", Rule("Variável errada", "QuoteSent", [], [new { kind = "SendEmail", message = "Olá {nome}" }]), CancellationToken), HttpStatusCode.UnprocessableEntity, "automations.invalid");
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/automations/rules", Rule("Gatilho", "Nada", [], [new { kind = "NotifyTeam", message = "Oi" }]), CancellationToken), HttpStatusCode.BadRequest, "automations.definition");

        using var created = await manager.PostAsJsonAsync("/api/admin/automations/rules", Rule("Aviso de diagnóstico", "ServiceOrderStatusChanged",
            [new { field = "orderStatus", @operator = "equals", value = "InDiagnosis" }],
            [new { kind = "NotifyTeam", message = "OS {os} de {cliente} em diagnóstico" }, new { kind = "SendEmail", message = "Olá {cliente}, seu {veiculo} está em diagnóstico." }]), CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var ruleId = (await ReadJsonAsync(created)).GetProperty("id").GetGuid();
        using var restock = await manager.PostAsJsonAsync("/api/admin/automations/rules", Rule("Reposição", "StockBelowMinimum", [], [new { kind = "CreatePurchaseOrder", message = (string?)null }]), CancellationToken);
        var restockId = (await ReadJsonAsync(restock)).GetProperty("id").GetGuid();

        var customer = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/customers", new { name = "Maria Souza", document = "529.982.247-25", phone = "(11) 98765-4321", email = "maria@exemplo.com", messagingConsent = new { whatsApp = false, email = true } }, CancellationToken));
        var customerId = customer.GetProperty("id").GetGuid();
        var vehicle = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/vehicles", new { customerId, licensePlate = "BRA2E19", brand = "Fiat", model = "Uno", year = 2020, mileage = 50_000 }, CancellationToken));
        await admin.PostAsJsonAsync("/api/admin/parts", new { code = "PST-001", name = "Pastilha", type = "Part", unitOfMeasure = "un", unitPrice = 80m, cost = 40m, minimumStock = 4m }, CancellationToken);
        var order = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/service-orders", new { customerId, vehicleId = vehicle.GetProperty("id").GetGuid(), problemDescription = "Freio chiando" }, CancellationToken));
        await mechanic.PostAsync($"/api/admin/service-orders/{order.GetProperty("id").GetGuid()}/start-service", null, CancellationToken);

        await WaitForAsync($"SELECT id FROM automationExecutions WHERE ruleId = '{ruleId}'");
        await RunAutomationsAsync();
        Assert.Contains(Factory.Emails.Messages, message => message.To == "maria@exemplo.com" && message.HtmlBody.Contains("Fiat Uno", StringComparison.Ordinal));
        var notifications = await ReadJsonAsync(await mechanic.GetAsync("/api/admin/notifications", CancellationToken));
        Assert.Contains(notifications.GetProperty("items").EnumerateArray(), item => item.GetProperty("title").GetString() == $"OS {order.GetProperty("number").GetInt64()} de Maria em diagnóstico");

        var rules = (await ReadJsonAsync(await manager.GetAsync("/api/admin/automations/rules", CancellationToken))).GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(1, rules.Single(rule => rule.GetProperty("id").GetGuid() == ruleId).GetProperty("runs24h").GetInt32());
        var failures = (await ReadJsonAsync(await manager.GetAsync("/api/admin/automations/queues/failures", CancellationToken))).GetProperty("items").EnumerateArray().ToList();
        var failure = Assert.Single(failures);
        Assert.Equal("automations", failure.GetProperty("queue").GetString());
        Assert.Contains("fornecedor", failure.GetProperty("error").GetString(), StringComparison.Ordinal);
        Assert.Contains((await ReadJsonAsync(await manager.GetAsync("/api/admin/notifications", CancellationToken))).GetProperty("items").EnumerateArray(), item => item.GetProperty("type").GetString() == "AutomationFailed");

        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsync($"/api/admin/automations/queues/failures/{failure.GetProperty("id").GetGuid()}/retry", null, CancellationToken)).StatusCode);
        await admin.PostAsJsonAsync("/api/admin/inventory/suppliers", new { name = "Auto Peças Centro", document = (string?)null, email = "pedidos@autopecas.example.com", phone = (string?)null, leadTimeDays = 2 }, CancellationToken);
        Assert.Equal(HttpStatusCode.Accepted, (await admin.PostAsync($"/api/admin/automations/queues/failures/{failure.GetProperty("id").GetGuid()}/retry", null, CancellationToken)).StatusCode);
        await AssertProblemAsync(await admin.PostAsync($"/api/admin/automations/queues/failures/{Guid.CreateVersion7()}/retry", null, CancellationToken), HttpStatusCode.NotFound, "automations.notFound");
        await RunAutomationsAsync();
        Assert.Empty((await ReadJsonAsync(await manager.GetAsync("/api/admin/automations/queues/failures", CancellationToken))).GetProperty("items").EnumerateArray());
        Assert.Single((await ReadJsonAsync(await admin.GetAsync("/api/admin/inventory/purchase-orders", CancellationToken))).GetProperty("items").EnumerateArray());

        var queues = (await ReadJsonAsync(await manager.GetAsync("/api/admin/automations/queues", CancellationToken))).GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(7, queues.Count);
        Assert.Equal(2, queues.Single(queue => queue.GetProperty("name").GetString() == "automations").GetProperty("processed").GetInt32());

        var disabled = await ReadJsonAsync(await manager.PutAsJsonAsync($"/api/admin/automations/rules/{restockId}/status", new { enabled = false }, CancellationToken));
        Assert.False(disabled.GetProperty("enabled").GetBoolean());
        await AssertProblemAsync(await manager.PutAsJsonAsync($"/api/admin/automations/rules/{restockId}/status", new { enabled = (bool?)null }, CancellationToken), HttpStatusCode.BadRequest, "automations.enabled");
        var renamed = await ReadJsonAsync(await manager.PutAsJsonAsync($"/api/admin/automations/rules/{restockId}", Rule("Reposição automática", "StockBelowMinimum", [new { field = "partStock", @operator = "lessThan", value = "2" }], [new { kind = "CreatePurchaseOrder", message = (string?)null }], new { amount = 2, unit = "hours" }), CancellationToken));
        Assert.Equal("Reposição automática", renamed.GetProperty("definition").GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await manager.DeleteAsync($"/api/admin/automations/rules/{restockId}", CancellationToken)).StatusCode);
        await AssertProblemAsync(await manager.DeleteAsync($"/api/admin/automations/rules/{restockId}", CancellationToken), HttpStatusCode.NotFound, "automations.notFound");
        Assert.Equal(HttpStatusCode.Forbidden, (await mechanic.GetAsync("/api/admin/automations/rules", CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Notifications_ReadAndPreferences_AndHubRequiresToken()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var attendant = await ClientAsync(Role.Attendant);
        var quote = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/reports/exports", new { definition = new { measure = "revenue", groupBy = "month", period = new { preset = "thisMonth", from = (string?)null, to = (string?)null }, chart = "column" }, format = "pdf" }, CancellationToken));
        Assert.Equal("Ready", quote.GetProperty("status").GetString());

        var list = (await ReadJsonAsync(await attendant.GetAsync("/api/admin/notifications", CancellationToken))).GetProperty("items").EnumerateArray().ToList();
        var item = Assert.Single(list);
        Assert.Equal("ExportReady", item.GetProperty("type").GetString());
        Assert.False(item.GetProperty("read").GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, (await attendant.PostAsync($"/api/admin/notifications/{item.GetProperty("id").GetGuid()}/read", null, CancellationToken)).StatusCode);
        Assert.True((await ReadJsonAsync(await attendant.GetAsync("/api/admin/notifications", CancellationToken))).GetProperty("items")[0].GetProperty("read").GetBoolean());
        Assert.False((await ReadJsonAsync(await admin.GetAsync("/api/admin/notifications", CancellationToken))).GetProperty("items")[0].GetProperty("read").GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync("/api/admin/notifications/read-all", null, CancellationToken)).StatusCode);
        Assert.True((await ReadJsonAsync(await admin.GetAsync("/api/admin/notifications", CancellationToken))).GetProperty("items")[0].GetProperty("read").GetBoolean());
        await AssertProblemAsync(await admin.PostAsync($"/api/admin/notifications/{Guid.CreateVersion7()}/read", null, CancellationToken), HttpStatusCode.NotFound, "notifications.notFound");

        Assert.Empty((await ReadJsonAsync(await attendant.GetAsync("/api/admin/notifications/preferences", CancellationToken))).GetProperty("items").EnumerateArray());
        await AssertProblemAsync(await attendant.PutAsJsonAsync("/api/admin/notifications/preferences", new { items = new[] { new { type = "Nada", inApp = true, email = false, whatsApp = false } } }, CancellationToken), HttpStatusCode.BadRequest, "notifications.preferences");
        var saved = await ReadJsonAsync(await attendant.PutAsJsonAsync("/api/admin/notifications/preferences", new { items = new[] { new { type = "ExportReady", inApp = false, email = true, whatsApp = false } } }, CancellationToken));
        Assert.True(saved.GetProperty("items")[0].GetProperty("email").GetBoolean());
        Assert.Empty((await ReadJsonAsync(await attendant.GetAsync("/api/admin/notifications", CancellationToken))).GetProperty("items").EnumerateArray());
        await admin.PostAsJsonAsync("/api/admin/reports/exports", new { definition = new { measure = "revenue", groupBy = "month", period = new { preset = "thisMonth", from = (string?)null, to = (string?)null }, chart = "column" }, format = "xlsx" }, CancellationToken);
        Assert.Contains(Factory.Emails.Messages, message => message.Subject == "Exportação de relatório pronta");

        using var anonymous = Factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync("/api/hubs/realtime/negotiate?negotiateVersion=1", null, CancellationToken)).StatusCode);
        using var negotiate = await attendant.PostAsync("/api/hubs/realtime/negotiate?negotiateVersion=1", null, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, negotiate.StatusCode);
        Assert.Equal(JsonValueKind.String, (await ReadJsonAsync(negotiate)).GetProperty("connectionToken").ValueKind);
    }
}
