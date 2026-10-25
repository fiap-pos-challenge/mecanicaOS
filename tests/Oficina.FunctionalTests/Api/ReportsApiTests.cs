using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Reports;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class ReportsApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    private static object Definition(string measure = "revenue", string groupBy = "month", string chart = "column", string preset = "thisMonth", string? from = null, string? to = null) =>
        new { measure, groupBy, period = new { preset, from, to }, chart };

    private async Task DeliveredOrderAsync(HttpClient admin, HttpClient mechanic)
    {
        var customer = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/customers", new { name = "Maria Souza", document = "529.982.247-25", phone = "(11) 98765-4321" }, CancellationToken));
        var customerId = customer.GetProperty("id").GetGuid();
        var vehicle = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/vehicles", new { customerId, licensePlate = "BRA2E19", brand = "Fiat", model = "Uno", year = 2020, mileage = 50_000 }, CancellationToken));
        var service = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/services", new { name = "Alinhamento 3D", basePrice = 120m, estimatedMinutes = 40, commissionPercent = 10m, warrantyDays = 90 }, CancellationToken));
        var part = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/parts", new { code = "PST-001", name = "Pastilha", type = "Part", unitOfMeasure = "un", unitPrice = 80m, cost = 40m, minimumStock = 0m }, CancellationToken));
        await Database.QueryAsync($"UPDATE parts SET stockQuantity = 10 WHERE id = '{part.GetProperty("id").GetGuid()}'");
        var order = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/service-orders", new { customerId, vehicleId = vehicle.GetProperty("id").GetGuid(), problemDescription = "Freio chiando" }, CancellationToken));
        var orderId = order.GetProperty("id").GetGuid();
        await mechanic.PostAsync($"/api/admin/service-orders/{orderId}/start-service", null, CancellationToken);
        await mechanic.PostAsJsonAsync($"/api/admin/service-orders/{orderId}/send-to-quote", new { description = "Trocar pastilha", items = new object[] { new { kind = "Service", itemId = service.GetProperty("id").GetGuid() }, new { kind = "Part", itemId = part.GetProperty("id").GetGuid() } } }, CancellationToken);
        await Database.QueryAsync($"UPDATE serviceOrders SET status = 'Delivered', deliveredAt = SYSDATETIMEOFFSET(), totalAmount = 200 WHERE id = '{orderId}'");
    }

    [Fact]
    public async Task Run_ComputesFromDeliveredOrders_AndRejectsBadDefinitions()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var mechanic = await ClientAsync(Role.Mechanic);
        using var manager = await ClientAsync(Role.Manager);
        await DeliveredOrderAsync(admin, mechanic);

        var revenue = await ReadJsonAsync(await manager.PostAsJsonAsync("/api/admin/reports/run", new { definition = Definition() }, CancellationToken));
        Assert.Equal("money", revenue.GetProperty("unit").GetString());
        Assert.Equal(20_000m, revenue.GetProperty("total").GetDecimal());
        var byService = await ReadJsonAsync(await manager.PostAsJsonAsync("/api/admin/reports/run", new { definition = Definition("commission", "service", "donut") }, CancellationToken));
        var row = Assert.Single(byService.GetProperty("rows").EnumerateArray());
        Assert.Equal("Alinhamento 3D", row.GetProperty("label").GetString());
        Assert.Equal(1_200m, row.GetProperty("value").GetDecimal());
        var margin = await ReadJsonAsync(await manager.PostAsJsonAsync("/api/admin/reports/run", new { definition = Definition("margin", "mechanic", "table", "yearToDate") }, CancellationToken));
        Assert.Equal(80m, margin.GetProperty("rows")[0].GetProperty("value").GetDecimal());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, margin.GetProperty("total").ValueKind);
        var category = await ReadJsonAsync(await manager.PostAsJsonAsync("/api/admin/reports/run", new { definition = Definition("partsOut", "category", "column") }, CancellationToken));
        Assert.Contains(category.GetProperty("rows").EnumerateArray(), item => item.GetProperty("label").GetString() == "Peças" && item.GetProperty("value").GetDecimal() == 1m);
        var weeks = await ReadJsonAsync(await manager.PostAsJsonAsync("/api/admin/reports/run", new { definition = Definition("orderCount", "week", "line", "custom", "2026-01-01", "2026-01-31") }, CancellationToken));
        Assert.Equal(5, weeks.GetProperty("rows").GetArrayLength());
        Assert.Equal(0m, weeks.GetProperty("total").GetDecimal());

        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/reports/run", new { definition = Definition("commission", "paymentMethod") }, CancellationToken), HttpStatusCode.UnprocessableEntity, "reports.incompatible");
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/reports/run", new { definition = Definition(chart: "line", groupBy: "customer") }, CancellationToken), HttpStatusCode.UnprocessableEntity, "reports.incompatible");
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/reports/run", new { definition = Definition(preset: "custom", from: "2026-02-01", to: "2026-01-01") }, CancellationToken), HttpStatusCode.UnprocessableEntity, "reports.incompatible");
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/reports/run", new { definition = Definition(measure: "lucro") }, CancellationToken), HttpStatusCode.BadRequest, "reports.definition");
        Assert.Equal(HttpStatusCode.Forbidden, (await mechanic.PostAsJsonAsync("/api/admin/reports/run", new { definition = Definition() }, CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Saved_CrudScheduleExportAndAssistant()
    {
        using var manager = await ClientAsync(Role.Manager);

        using var created = await manager.PostAsJsonAsync("/api/admin/reports/saved", new { name = "Faturamento mensal", definition = Definition() }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await ReadJsonAsync(created)).GetProperty("id").GetGuid();
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/reports/saved", new { name = "faturamento mensal", definition = Definition() }, CancellationToken), HttpStatusCode.Conflict, "reports.duplicateName");
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/reports/saved", new { name = "AB", definition = Definition() }, CancellationToken), HttpStatusCode.BadRequest, "reports.name");
        var updated = await ReadJsonAsync(await manager.PutAsJsonAsync($"/api/admin/reports/saved/{id}", new { name = "Ticket por mecânico", definition = Definition("averageTicket", "mechanic", "table") }, CancellationToken));
        Assert.Equal("averageTicket", updated.GetProperty("definition").GetProperty("measure").GetString());

        using var invalidSchedule = await manager.PutAsJsonAsync($"/api/admin/reports/saved/{id}/schedule", new { schedule = new { weekday = "Funday", time = "25:00", recipients = new[] { "x" }, format = "doc" } }, CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalidSchedule.StatusCode);
        Assert.Equal(4, (await ReadJsonAsync(invalidSchedule)).GetProperty("errors").GetArrayLength());
        var now = TimeProvider.System.GetUtcNow().ToOffset(TimeSpan.FromHours(-3));
        var scheduled = await ReadJsonAsync(await manager.PutAsJsonAsync($"/api/admin/reports/saved/{id}/schedule", new { schedule = new { weekday = now.DayOfWeek.ToString(), time = "00:00", recipients = new[] { "Dono@Oficina.com" }, format = "pdf" } }, CancellationToken));
        Assert.Equal("dono@oficina.com", scheduled.GetProperty("schedule").GetProperty("recipients")[0].GetString());
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ScheduledReportSender>();
            Assert.Equal(1, await service.SendDueAsync(CancellationToken));
            Assert.Equal(0, await service.SendDueAsync(CancellationToken));
        }

        Assert.Contains(Factory.Emails.Messages, message => message.To == "dono@oficina.com");
        var listed = Assert.Single((await ReadJsonAsync(await manager.GetAsync("/api/admin/reports/saved", CancellationToken))).GetProperty("items").EnumerateArray());
        Assert.Equal("pdf", listed.GetProperty("schedule").GetProperty("format").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, (await ReadJsonAsync(await manager.PutAsJsonAsync($"/api/admin/reports/saved/{id}/schedule", new { schedule = (object?)null }, CancellationToken))).GetProperty("schedule").ValueKind);

        using var export = await manager.PostAsJsonAsync("/api/admin/reports/exports", new { definition = Definition(), format = "xlsx" }, CancellationToken);
        Assert.Equal(HttpStatusCode.Accepted, export.StatusCode);
        var exportId = (await ReadJsonAsync(export)).GetProperty("id").GetGuid();
        var job = await ReadJsonAsync(await manager.GetAsync($"/api/admin/reports/exports/{exportId}", CancellationToken));
        Assert.Equal("Ready", job.GetProperty("status").GetString());
        Assert.StartsWith($"data:{ReportFiles.XlsxType};base64,UEsD", job.GetProperty("fileUrl").GetString(), StringComparison.Ordinal);
        var pdf = await ReadJsonAsync(await manager.PostAsJsonAsync("/api/admin/reports/exports", new { definition = Definition(), format = "pdf" }, CancellationToken));
        Assert.StartsWith("data:application/pdf;base64,JVBERi", pdf.GetProperty("fileUrl").GetString(), StringComparison.Ordinal);
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/reports/exports", new { definition = Definition(), format = "csv" }, CancellationToken), HttpStatusCode.BadRequest, "reports.format");
        await AssertProblemAsync(await manager.GetAsync($"/api/admin/reports/exports/{Guid.CreateVersion7()}", CancellationToken), HttpStatusCode.NotFound, "reports.notFound");

        var suggestion = await ReadJsonAsync(await manager.PostAsJsonAsync("/api/admin/assistant/report-definitions", new { question = "Qual o ticket médio por mecânico no trimestre?" }, CancellationToken));
        Assert.Equal("averageTicket", suggestion.GetProperty("definition").GetProperty("measure").GetString());
        Assert.Equal("mechanic", suggestion.GetProperty("definition").GetProperty("groupBy").GetString());
        Assert.Equal("lastQuarter", suggestion.GetProperty("definition").GetProperty("period").GetProperty("preset").GetString());
        Assert.Equal("Ticket médio por mecânico, último trimestre.", suggestion.GetProperty("explanation").GetString());
        var march = await ReadJsonAsync(await manager.PostAsJsonAsync("/api/admin/assistant/report-definitions", new { question = "faturamento por semana em março" }, CancellationToken));
        Assert.Equal("custom", march.GetProperty("definition").GetProperty("period").GetProperty("preset").GetString());
        Assert.Equal("line", march.GetProperty("definition").GetProperty("chart").GetString());
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/assistant/report-definitions", new { question = "como está o tempo hoje?" }, CancellationToken), HttpStatusCode.UnprocessableEntity, "assistant.notUnderstood");
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/assistant/report-definitions", new { question = "oi" }, CancellationToken), HttpStatusCode.BadRequest, "assistant.question");

        Assert.Equal(HttpStatusCode.NoContent, (await manager.DeleteAsync($"/api/admin/reports/saved/{id}", CancellationToken)).StatusCode);
        await AssertProblemAsync(await manager.DeleteAsync($"/api/admin/reports/saved/{id}", CancellationToken), HttpStatusCode.NotFound, "reports.notFound");
    }
}
