using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class ServiceOrderWorkflowApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    private const string Photo = "data:image/png;base64,iVBORw0KGgo=";

    private async Task<(Guid CustomerId, Guid VehicleId, Guid ServiceId, Guid PartId)> SeedAsync(HttpClient admin)
    {
        var customer = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/customers", new
        {
            name = "Maria Souza",
            document = "529.982.247-25",
            phone = "(11) 98765-4321",
            address = new { zipCode = "01310-100", street = "Av. Paulista", number = "1000", district = "Bela Vista", city = "São Paulo", state = "SP" },
        }, CancellationToken));
        var customerId = customer.GetProperty("id").GetGuid();
        var vehicle = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/vehicles", new { customerId, licensePlate = "BRA2E19", brand = "Fiat", model = "Uno", year = 2020, mileage = 50_000 }, CancellationToken));
        var service = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/services", new { name = "Alinhamento 3D", basePrice = 120m, estimatedMinutes = 40, commissionPercent = 10m, warrantyDays = 90 }, CancellationToken));
        var part = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/parts", new { code = "PST-001", name = "Pastilha de freio dianteira", type = "Part", unitOfMeasure = "un", unitPrice = 80m, cost = 40m, minimumStock = 0m }, CancellationToken));
        return (customerId, vehicle.GetProperty("id").GetGuid(), service.GetProperty("id").GetGuid(), part.GetProperty("id").GetGuid());
    }

    private static async Task<JsonElement> TransitionAsync(HttpClient client, Guid id, string to, int version, string? reason = null)
    {
        using var response = await client.PostAsJsonAsync($"/api/admin/service-orders/{id}/transitions", new { to, version, reason }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadJsonAsync(response);
    }

    [Fact]
    public async Task Workflow_FromIntakeToDelivery_WithMechanicSheetAndTimer()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var attendant = await ClientAsync(Role.Attendant);
        using var mechanic = await ClientAsync(Role.Mechanic);
        var (customerId, vehicleId, serviceId, partId) = await SeedAsync(admin);

        using var opened = await attendant.PostAsJsonAsync("/api/admin/service-orders", new
        {
            customerId,
            vehicleId,
            problemDescription = "Barulho na suspensão e freio chiando",
            promisedAt = TimeProvider.System.GetUtcNow().AddDays(2),
        }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, opened.StatusCode);
        var order = await ReadJsonAsync(opened);
        var id = order.GetProperty("id").GetGuid();
        Assert.Equal("Received", order.GetProperty("status").GetString());
        Assert.Equal(50_000, order.GetProperty("mileage").GetInt32());
        Assert.Equal("Fiat Uno 2020", order.GetProperty("vehicle").GetString());
        await AssertProblemAsync(
            await attendant.PostAsJsonAsync("/api/admin/service-orders", new { customerId, vehicleId, problemDescription = "Outra visita" }, CancellationToken),
            HttpStatusCode.Conflict,
            "serviceOrder.vehicleHasOpenOrder");

        var mine = await ReadJsonAsync(await mechanic.GetAsync("/api/admin/service-orders/mine", CancellationToken));
        Assert.Contains(mine.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == id);
        var started = await ReadJsonAsync(await mechanic.PostAsync($"/api/admin/service-orders/{id}/start-service", null, CancellationToken));
        Assert.Equal("InDiagnosis", started.GetProperty("status").GetString());
        var checklistId = started.GetProperty("checklist")[0].GetProperty("id").GetString();
        using var checklistResponse = await mechanic.PutAsJsonAsync($"/api/admin/service-orders/{id}/checklist", new { items = new[] { new { id = checklistId, state = "Attention" } } }, CancellationToken);
        Assert.True(checklistResponse.IsSuccessStatusCode, await checklistResponse.Content.ReadAsStringAsync(CancellationToken));
        var checklist = await ReadJsonAsync(checklistResponse);
        Assert.Equal("Attention", checklist.GetProperty("checklist")[0].GetProperty("state").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await mechanic.PutAsJsonAsync($"/api/admin/service-orders/{id}/checklist", new { items = new[] { new { id = checklistId, state = "Broken" } } }, CancellationToken)).StatusCode);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            await mechanic.PostAsJsonAsync($"/api/admin/service-orders/{id}/photos", new { clientId = "foto-1", fileName = "roda.png", contentType = "image/png", dataUrl = Photo }, CancellationToken);
        }

        await AssertProblemAsync(
            await mechanic.PostAsJsonAsync($"/api/admin/service-orders/{id}/photos", new { clientId = "foto-2", fileName = "x.gif", contentType = "image/gif", dataUrl = "data:image/gif;base64,R0" }, CancellationToken),
            HttpStatusCode.BadRequest,
            "serviceOrders.photoType");
        var work = await ReadJsonAsync(await mechanic.PutAsJsonAsync($"/api/admin/service-orders/{id}/diagnosis", new { description = "Pastilha gasta e bieleta com folga" }, CancellationToken));
        Assert.Single(work.GetProperty("photos").EnumerateArray());
        Assert.Equal("Pastilha gasta e bieleta com folga", work.GetProperty("diagnosis").GetString());

        var suggestions = (await ReadJsonAsync(await mechanic.PostAsJsonAsync($"/api/admin/service-orders/{id}/diagnosis/suggestions", new { description = "freio chiando e suspensão com folga" }, CancellationToken)))
            .GetProperty("items").EnumerateArray().Select(item => item.GetProperty("itemId").GetGuid()).ToArray();
        Assert.Contains(serviceId, suggestions);
        Assert.Contains(partId, suggestions);
        Assert.Equal(HttpStatusCode.BadRequest, (await mechanic.PostAsJsonAsync($"/api/admin/service-orders/{id}/diagnosis/suggestions", new { description = "x" }, CancellationToken)).StatusCode);

        var current = await ReadJsonAsync(await attendant.GetAsync($"/api/admin/service-orders/{id}", CancellationToken));
        await AssertProblemAsync(
            await attendant.PostAsJsonAsync($"/api/admin/service-orders/{id}/transitions", new { to = "WaitingApproval", version = current.GetProperty("version").GetInt32() }, CancellationToken),
            HttpStatusCode.Conflict,
            "serviceOrder.quoteWithoutItems");

        await AssertProblemAsync(
            await mechanic.PostAsJsonAsync($"/api/admin/service-orders/{id}/send-to-quote", new { description = "Trocar pastilha e alinhar", items = new object[] { new { kind = "Part", itemId = partId } } }, CancellationToken),
            HttpStatusCode.UnprocessableEntity,
            "inventory.partUnavailable");

        await Database.QueryAsync($"UPDATE parts SET stockQuantity = 10 WHERE id = '{partId}'");
        var sentToQuote = await ReadJsonAsync(await mechanic.PostAsJsonAsync($"/api/admin/service-orders/{id}/send-to-quote", new { description = "Trocar pastilha e alinhar", items = new object[] { new { kind = "Service", itemId = serviceId }, new { kind = "Part", itemId = partId } } }, CancellationToken));
        Assert.True(sentToQuote.GetProperty("sentToQuote").GetBoolean());

        current = await ReadJsonAsync(await attendant.GetAsync($"/api/admin/service-orders/{id}", CancellationToken));
        var version = current.GetProperty("version").GetInt32();
        Assert.True(current.GetProperty("hasQuoteItems").GetBoolean());
        await AssertProblemAsync(
            await attendant.PostAsJsonAsync($"/api/admin/service-orders/{id}/transitions", new { to = "WaitingApproval", version = version - 1 }, CancellationToken),
            HttpStatusCode.Conflict,
            "serviceOrders.versionConflict");
        await AssertProblemAsync(
            await attendant.PostAsJsonAsync($"/api/admin/service-orders/{id}/transitions", new { to = "Delivered", version }, CancellationToken),
            HttpStatusCode.Conflict,
            "serviceOrder.invalidTransition");

        var waiting = await TransitionAsync(attendant, id, "WaitingApproval", version);
        Assert.Equal(200m, waiting.GetProperty("quote").GetProperty("total").GetDecimal());
        Assert.Equal(2, waiting.GetProperty("quote").GetProperty("items").GetArrayLength());
        await AssertProblemAsync(
            await attendant.PostAsJsonAsync($"/api/admin/service-orders/{id}/transitions", new { to = "InDiagnosis", version = waiting.GetProperty("version").GetInt32(), reason = "caro" }, CancellationToken),
            HttpStatusCode.BadRequest,
            "serviceOrder.rejectionReasonRequired");
        var rejected = await TransitionAsync(attendant, id, "InDiagnosis", waiting.GetProperty("version").GetInt32(), "Cliente achou caro");
        Assert.Equal("Rejected", rejected.GetProperty("quote").GetProperty("status").GetString());
        var resent = await TransitionAsync(attendant, id, "WaitingApproval", rejected.GetProperty("version").GetInt32());
        var approved = await TransitionAsync(attendant, id, "InProgress", resent.GetProperty("version").GetInt32());
        Assert.Equal("Approved", approved.GetProperty("quote").GetProperty("status").GetString());
        Assert.True(approved.GetProperty("parts")[0].GetProperty("reserved").GetBoolean());

        var timing = await ReadJsonAsync(await mechanic.PostAsync($"/api/admin/service-orders/{id}/time-entries/start", null, CancellationToken));
        Assert.Single(timing.GetProperty("timeEntries").EnumerateArray());
        Assert.Equal("Mechanic User", timing.GetProperty("mechanicName").GetString());
        var stopped = await ReadJsonAsync(await mechanic.PostAsync($"/api/admin/service-orders/{id}/time-entries/stop", null, CancellationToken));
        Assert.NotEqual(JsonValueKind.Null, stopped.GetProperty("timeEntries")[0].GetProperty("endedAt").ValueKind);
        await mechanic.PostAsync($"/api/admin/service-orders/{id}/start-service", null, CancellationToken);
        Assert.True((await ReadJsonAsync(await mechanic.GetAsync($"/api/admin/service-orders/{id}/work", CancellationToken))).GetProperty("timerRunning").GetBoolean());
        var finished = await ReadJsonAsync(await mechanic.PostAsync($"/api/admin/service-orders/{id}/finish-service", null, CancellationToken));
        Assert.Equal("Completed", finished.GetProperty("status").GetString());
        Assert.False(finished.GetProperty("timerRunning").GetBoolean());
        await AssertProblemAsync(await mechanic.PostAsync($"/api/admin/service-orders/{id}/finish-service", null, CancellationToken), HttpStatusCode.Conflict, "serviceOrder.invalidTransition");
        await AssertProblemAsync(await mechanic.PostAsync($"/api/admin/service-orders/{id}/time-entries/start", null, CancellationToken), HttpStatusCode.Conflict, "serviceOrder.invalidTransition");

        current = await ReadJsonAsync(await attendant.GetAsync($"/api/admin/service-orders/{id}", CancellationToken));
        var delivered = await TransitionAsync(attendant, id, "Delivered", current.GetProperty("version").GetInt32());
        Assert.Equal("Delivered", delivered.GetProperty("status").GetString());
        Assert.DoesNotContain((await ReadJsonAsync(await mechanic.GetAsync("/api/admin/service-orders/mine", CancellationToken))).GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == id);

        var board = await ReadJsonAsync(await attendant.GetAsync("/api/admin/service-orders?page=1&pageSize=10", CancellationToken));
        var card = Assert.Single(board.GetProperty("items").EnumerateArray());
        Assert.Equal("Maria Souza", card.GetProperty("customerName").GetString());
        Assert.Equal("Mechanic User", card.GetProperty("mechanicName").GetString());

        var productivity = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/team/productivity", new { userIds = new[] { timing.GetProperty("timeEntries")[0].GetProperty("mechanicId").GetGuid() }, periodDays = 30 }, CancellationToken));
        var mechanicStats = Assert.Single(productivity.GetProperty("items").EnumerateArray());
        Assert.Equal(1, mechanicStats.GetProperty("serviceOrders").GetInt32());
        Assert.Equal(200m, mechanicStats.GetProperty("revenue").GetDecimal());
        Assert.Equal(4, mechanicStats.GetProperty("weekly").GetArrayLength());
    }

    [Fact]
    public async Task Open_InvalidRequests_ReturnTheErrors()
    {
        using var admin = await ClientAsync(Role.Admin);
        var (customerId, vehicleId, _, _) = await SeedAsync(admin);

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/service-orders", new { customerId, vehicleId, problemDescription = "x" }, CancellationToken)).StatusCode);
        await AssertProblemAsync(await admin.PostAsJsonAsync("/api/admin/service-orders", new { customerId = Guid.CreateVersion7(), vehicleId, problemDescription = "Barulho" }, CancellationToken), HttpStatusCode.NotFound, "customer.notFound");
        await AssertProblemAsync(await admin.PostAsJsonAsync("/api/admin/service-orders", new { customerId, vehicleId = Guid.CreateVersion7(), problemDescription = "Barulho" }, CancellationToken), HttpStatusCode.NotFound, "vehicle.notFound");
        await AssertProblemAsync(await admin.PostAsJsonAsync("/api/admin/service-orders", new { customerId, vehicleId, problemDescription = "Barulho", promisedAt = TimeProvider.System.GetUtcNow().AddDays(-1) }, CancellationToken), HttpStatusCode.BadRequest, "serviceOrders.invalidPromise");
        var unknown = Guid.CreateVersion7();
        await AssertProblemAsync(await admin.PostAsJsonAsync($"/api/admin/service-orders/{unknown}/transitions", new { to = "InDiagnosis", version = 0 }, CancellationToken), HttpStatusCode.NotFound, "serviceOrder.notFound");
        await AssertProblemAsync(await admin.GetAsync($"/api/admin/service-orders/{unknown}/work", CancellationToken), HttpStatusCode.NotFound, "serviceOrder.notFound");
        await AssertProblemAsync(await admin.PostAsync($"/api/admin/service-orders/{unknown}/start-service", null, CancellationToken), HttpStatusCode.NotFound, "serviceOrder.notFound");
    }

    [Fact]
    public async Task TeamProfiles_SaveReadAndRejectInvalidData()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var manager = await ClientAsync(Role.Manager);
        var mechanic = await SaveUserAsync(Role.Mechanic);
        var attendant = await SaveUserAsync(Role.Attendant);

        var empty = await ReadJsonAsync(await manager.GetAsync($"/api/admin/team/{mechanic.Id}/profile", CancellationToken));
        Assert.Empty(empty.GetProperty("specialties").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, empty.GetProperty("updatedAt").ValueKind);

        var schedule = new[] { new { day = "Monday", start = "08:00", end = "17:00" }, new { day = "Tuesday", start = "08:00", end = "12:00" } };
        using var saved = await admin.PutAsJsonAsync($"/api/admin/team/{mechanic.Id}/profile", new { specialties = new[] { " Suspensão ", "Freios" }, commissionPercent = 12.5m, discountLimitPercent = 0m, schedule }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var listed = Assert.Single((await ReadJsonAsync(await manager.PostAsJsonAsync("/api/admin/team/profiles/query", new { userIds = new[] { mechanic.Id } }, CancellationToken))).GetProperty("items").EnumerateArray());
        Assert.Equal("Suspensão", listed.GetProperty("specialties")[0].GetString());
        Assert.Equal("17:00", listed.GetProperty("schedule")[0].GetProperty("end").GetString());

        await AssertProblemAsync(await admin.PutAsJsonAsync($"/api/admin/team/{mechanic.Id}/profile", new { specialties = Array.Empty<string>(), commissionPercent = 10m, discountLimitPercent = 5m, schedule = Array.Empty<object>() }, CancellationToken), HttpStatusCode.UnprocessableEntity, "team.mechanicDiscount");
        await AssertProblemAsync(await admin.PutAsJsonAsync($"/api/admin/team/{attendant.Id}/profile", new { specialties = Array.Empty<string>(), commissionPercent = 60m, discountLimitPercent = 5m, schedule = Array.Empty<object>() }, CancellationToken), HttpStatusCode.BadRequest, "team.invalidCommission");
        await AssertProblemAsync(await admin.PutAsJsonAsync($"/api/admin/team/{attendant.Id}/profile", new { specialties = new[] { "x" }, commissionPercent = 10m, discountLimitPercent = 5m, schedule = Array.Empty<object>() }, CancellationToken), HttpStatusCode.BadRequest, "team.invalidSpecialties");
        await AssertProblemAsync(await admin.PutAsJsonAsync($"/api/admin/team/{attendant.Id}/profile", new { specialties = Array.Empty<string>(), commissionPercent = 10m, discountLimitPercent = 40m, schedule = Array.Empty<object>() }, CancellationToken), HttpStatusCode.BadRequest, "team.invalidDiscountLimit");
        await AssertProblemAsync(await admin.PutAsJsonAsync($"/api/admin/team/{attendant.Id}/profile", new { specialties = Array.Empty<string>(), commissionPercent = 10m, discountLimitPercent = 5m, schedule = new[] { new { day = "Monday", start = "18:00", end = "08:00" } } }, CancellationToken), HttpStatusCode.BadRequest, "team.invalidShift");
        await AssertProblemAsync(await admin.PutAsJsonAsync($"/api/admin/team/{attendant.Id}/profile", new { specialties = Array.Empty<string>(), commissionPercent = 10m, discountLimitPercent = 5m, schedule = new[] { new { day = "Funday", start = "08:00", end = "09:00" } } }, CancellationToken), HttpStatusCode.BadRequest, "team.invalidShift");
        await AssertProblemAsync(await admin.PutAsJsonAsync($"/api/admin/team/{Guid.CreateVersion7()}/profile", new { specialties = Array.Empty<string>(), commissionPercent = 10m, discountLimitPercent = 5m, schedule = Array.Empty<object>() }, CancellationToken), HttpStatusCode.NotFound, "identity.notFound");
        Assert.Equal(HttpStatusCode.BadRequest, (await manager.PostAsJsonAsync("/api/admin/team/productivity", new { userIds = new[] { mechanic.Id }, periodDays = 45 }, CancellationToken)).StatusCode);
        var stats = Assert.Single((await ReadJsonAsync(await manager.PostAsJsonAsync("/api/admin/team/productivity", new { userIds = new[] { mechanic.Id }, periodDays = 90 }, CancellationToken))).GetProperty("items").EnumerateArray());
        Assert.Equal(0, stats.GetProperty("serviceOrders").GetInt32());
        Assert.Equal(13, stats.GetProperty("weekly").GetArrayLength());
    }

    [Fact]
    public async Task Roles_AreListedAndNotCustomizable()
    {
        using var admin = await ClientAsync(Role.Admin);

        var roles = await ReadJsonAsync(await admin.GetAsync("/api/admin/roles", CancellationToken));
        Assert.False(roles.GetProperty("customizable").GetBoolean());
        var mechanic = roles.GetProperty("items").EnumerateArray().Single(item => item.GetProperty("role").GetString() == "Mechanic");
        Assert.Contains(mechanic.GetProperty("permissions").EnumerateArray(), permission => permission.GetString() == "mechanic.view");
        await AssertProblemAsync(await admin.PutAsJsonAsync("/api/admin/roles/Mechanic/permissions", new { permissions = new[] { "catalog.view" } }, CancellationToken), HttpStatusCode.UnprocessableEntity, "roles.customizationUnavailable");
    }
}
