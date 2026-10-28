using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class ServiceOrderFlowApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    private const string Document = "52998224725";
    private const string Plate = "BRA2E19";

    [Fact]
    public async Task FullFlow_FromRegistrationToDelivery_WorksThroughTheApi()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var attendant = await ClientAsync(Role.Attendant);
        using var mechanic = await ClientAsync(Role.Mechanic);
        using var customerApp = Factory.CreateClient();

        var customer = await SendAsync(admin, HttpMethod.Post, "/api/admin/customers", HttpStatusCode.Created,
            new { name = "Maria Souza", document = Document, email = "maria@exemplo.com", phone = "11987654321" });
        await SendAsync(attendant, HttpMethod.Post, "/api/admin/vehicles", HttpStatusCode.Created,
            new { plate = Plate, brand = "Volkswagen", model = "Gol", year = 2020, customerId = customer.GetProperty("id").GetGuid() });
        var service = await SendAsync(admin, HttpMethod.Post, "/api/admin/services", HttpStatusCode.Created,
            new { name = "Troca de óleo", basePrice = 150m, estimatedMinutes = 45 });
        var part = await SendAsync(admin, HttpMethod.Post, "/api/admin/parts", HttpStatusCode.Created,
            new { code = "flt-001", name = "Filtro de óleo", type = "Part", unitOfMeasure = "un", unitPrice = 25m, minimumStock = 2m });
        var partId = part.GetProperty("id").GetGuid();
        Assert.Equal("FLT-001", part.GetProperty("code").GetString());
        Assert.True(part.GetProperty("isBelowMinimum").GetBoolean());

        var stocked = await SendAsync(admin, HttpMethod.Post, $"/api/admin/parts/{partId}/stock-entries", HttpStatusCode.OK,
            new { quantity = 10m, reason = "Compra NF 123" });
        Assert.Equal(10m, stocked.GetProperty("stockQuantity").GetDecimal());

        var opened = await SendAsync(attendant, HttpMethod.Post, "/api/admin/service-orders", HttpStatusCode.Created,
            new { customerDocument = Document, licensePlate = Plate, problemDescription = "Barulho ao frear" });
        var id = opened.GetProperty("id").GetGuid();
        var number = opened.GetProperty("number").GetInt64();
        Assert.Equal("Received", opened.GetProperty("status").GetString());
        Assert.Equal("Maria Souza", opened.GetProperty("customer").GetProperty("name").GetString());

        await AssertProblemAsync(
            await attendant.PostAsJsonAsync("/api/admin/service-orders", new { customerDocument = Document, licensePlate = Plate, problemDescription = "Outra visita" }, CancellationToken),
            HttpStatusCode.Conflict,
            "serviceOrder.vehicleHasOpenOrder");

        var diagnosing = await SendAsync(mechanic, HttpMethod.Post, $"/api/admin/service-orders/{id}/diagnosis/start", HttpStatusCode.OK);
        Assert.Equal("InDiagnosis", diagnosing.GetProperty("status").GetString());

        await SendAsync(mechanic, HttpMethod.Post, $"/api/admin/service-orders/{id}/services", HttpStatusCode.OK,
            new { serviceId = service.GetProperty("id").GetGuid(), quantity = 1 });
        var withParts = await SendAsync(mechanic, HttpMethod.Post, $"/api/admin/service-orders/{id}/parts", HttpStatusCode.OK,
            new { partId, quantity = 2m });
        Assert.Equal(50m, Assert.Single(withParts.GetProperty("parts").EnumerateArray()).GetProperty("total").GetDecimal());

        await AssertProblemAsync(
            await mechanic.PostAsJsonAsync($"/api/admin/service-orders/{id}/parts", new { partId, quantity = 20m }, CancellationToken),
            HttpStatusCode.UnprocessableEntity,
            "inventory.partUnavailable");

        var firstQuote = await SendAsync(attendant, HttpMethod.Post, $"/api/admin/service-orders/{id}/quote/send", HttpStatusCode.OK);
        var firstToken = firstQuote.GetProperty("approvalToken").GetString()!;
        Assert.Equal("WaitingApproval", firstQuote.GetProperty("serviceOrder").GetProperty("status").GetString());
        Assert.Equal(200m, firstQuote.GetProperty("serviceOrder").GetProperty("quote").GetProperty("total").GetDecimal());

        var rejected = await SendAsync(customerApp, HttpMethod.Post, $"/api/public/quotes/{firstToken}/reject", HttpStatusCode.OK,
            new { reason = "Vou trazer o filtro" });
        Assert.Equal("InDiagnosis", rejected.GetProperty("status").GetString());
        Assert.Equal("Rejected", rejected.GetProperty("quote").GetProperty("status").GetString());
        await AssertProblemAsync(
            await customerApp.PostAsync($"/api/public/quotes/{firstToken}/approve", null, CancellationToken),
            HttpStatusCode.NotFound,
            "serviceOrder.notFound");

        var partItemId = withParts.GetProperty("parts")[0].GetProperty("id").GetGuid();
        var withoutPart = await SendAsync(mechanic, HttpMethod.Delete, $"/api/admin/service-orders/{id}/items/{partItemId}", HttpStatusCode.OK);
        Assert.Empty(withoutPart.GetProperty("parts").EnumerateArray());
        await SendAsync(mechanic, HttpMethod.Post, $"/api/admin/service-orders/{id}/parts", HttpStatusCode.OK, new { partId, quantity = 1m });

        var secondQuote = await SendAsync(attendant, HttpMethod.Post, $"/api/admin/service-orders/{id}/quote/send", HttpStatusCode.OK);
        Assert.Equal(175m, secondQuote.GetProperty("serviceOrder").GetProperty("quote").GetProperty("total").GetDecimal());

        var approved = await SendAsync(customerApp, HttpMethod.Post,
            $"/api/public/quotes/{secondQuote.GetProperty("approvalToken").GetString()}/approve", HttpStatusCode.OK);
        Assert.Equal("InProgress", approved.GetProperty("status").GetString());

        var stock = await SendAsync(mechanic, HttpMethod.Get, $"/api/admin/parts/{partId}", HttpStatusCode.OK);
        Assert.Equal(9m, stock.GetProperty("stockQuantity").GetDecimal());
        Assert.Equal(new[] { "Entry", "Deduction" }, stock.GetProperty("stockMovements").EnumerateArray().Select(movement => movement.GetProperty("type").GetString()).ToArray());

        Assert.Equal("Completed", (await SendAsync(mechanic, HttpMethod.Post, $"/api/admin/service-orders/{id}/complete", HttpStatusCode.OK)).GetProperty("status").GetString());
        var delivered = await SendAsync(attendant, HttpMethod.Post, $"/api/admin/service-orders/{id}/deliver", HttpStatusCode.OK);
        Assert.Equal("Delivered", delivered.GetProperty("status").GetString());
        Assert.Equal(
            new[] { "Received", "InDiagnosis", "WaitingApproval", "InDiagnosis", "WaitingApproval", "InProgress", "Completed", "Delivered" },
            delivered.GetProperty("statusHistory").EnumerateArray().Select(entry => entry.GetProperty("newStatus").GetString()).ToArray());
        Assert.Equal("Vou trazer o filtro", delivered.GetProperty("statusHistory")[3].GetProperty("note").GetString());

        var tracked = await SendAsync(customerApp, HttpMethod.Post, "/api/public/service-orders/lookup", HttpStatusCode.OK,
            new { number, document = Document });
        Assert.Equal("Delivered", tracked.GetProperty("status").GetString());

        var metrics = await SendAsync(admin, HttpMethod.Get, "/api/admin/metrics/average-execution-time", HttpStatusCode.OK);
        Assert.Equal(1, metrics.GetProperty("totalServiceOrders").GetInt32());

        await SendAsync(attendant, HttpMethod.Post, "/api/admin/service-orders", HttpStatusCode.Created,
            new { customerDocument = Document, licensePlate = Plate, problemDescription = "Revisão de 10 mil km" });
    }

    [Fact]
    public async Task InPersonRejection_ReturnsToDiagnosisAndInPersonApprovalStartsExecution()
    {
        using var admin = await ClientAsync(Role.Admin);
        var id = await OpenInDiagnosisWithServiceAsync(admin);

        await SendAsync(admin, HttpMethod.Post, $"/api/admin/service-orders/{id}/quote/send", HttpStatusCode.OK);
        await AssertProblemAsync(
            await admin.PostAsJsonAsync($"/api/admin/service-orders/{id}/quote/reject", new { reason = " " }, CancellationToken),
            HttpStatusCode.BadRequest,
            "common.validationFailed");
        var rejected = await SendAsync(admin, HttpMethod.Post, $"/api/admin/service-orders/{id}/quote/reject", HttpStatusCode.OK, new { reason = "Cliente pediu outra peça" });
        Assert.Equal("InDiagnosis", rejected.GetProperty("status").GetString());

        await SendAsync(admin, HttpMethod.Post, $"/api/admin/service-orders/{id}/quote/send", HttpStatusCode.OK);
        var approved = await SendAsync(admin, HttpMethod.Post, $"/api/admin/service-orders/{id}/quote/approve", HttpStatusCode.OK);
        Assert.Equal("InProgress", approved.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, approved.GetProperty("executionStartedAt").ValueKind);
    }

    [Fact]
    public async Task Transitions_OutOfOrder_Return409()
    {
        using var admin = await ClientAsync(Role.Admin);
        var id = await OpenInDiagnosisWithServiceAsync(admin);

        await AssertProblemAsync(await admin.PostAsync($"/api/admin/service-orders/{id}/diagnosis/start", null, CancellationToken), HttpStatusCode.Conflict, "serviceOrder.invalidTransition");
        await AssertProblemAsync(await admin.PostAsync($"/api/admin/service-orders/{id}/complete", null, CancellationToken), HttpStatusCode.Conflict, "serviceOrder.invalidTransition");
        await AssertProblemAsync(await admin.PostAsync($"/api/admin/service-orders/{id}/deliver", null, CancellationToken), HttpStatusCode.Conflict, "serviceOrder.invalidTransition");
        await AssertProblemAsync(await admin.PostAsync($"/api/admin/service-orders/{id}/quote/approve", null, CancellationToken), HttpStatusCode.Conflict, "serviceOrder.invalidTransition");
        await AssertProblemAsync(await admin.DeleteAsync($"/api/admin/service-orders/{id}/items/{Guid.CreateVersion7()}", CancellationToken), HttpStatusCode.NotFound, "serviceOrder.itemNotFound");

        await SendAsync(admin, HttpMethod.Post, $"/api/admin/service-orders/{id}/quote/send", HttpStatusCode.OK);
        await AssertProblemAsync(
            await admin.PostAsJsonAsync($"/api/admin/service-orders/{id}/services", new { serviceId = Guid.CreateVersion7(), quantity = 1 }, CancellationToken),
            HttpStatusCode.NotFound,
            "catalog.notFound");
        var services = await SendAsync(admin, HttpMethod.Get, "/api/admin/services", HttpStatusCode.OK);
        await AssertProblemAsync(
            await admin.PostAsJsonAsync($"/api/admin/service-orders/{id}/services", new { serviceId = services.GetProperty("items")[0].GetProperty("id").GetGuid(), quantity = 1 }, CancellationToken),
            HttpStatusCode.Conflict,
            "serviceOrder.itemsOnlyInDiagnosis");
    }

    [Fact]
    public async Task Open_UnknownCustomerOrVehicleOrOtherOwner_ReturnsTheError()
    {
        using var admin = await ClientAsync(Role.Admin);
        await SaveCustomerWithVehicleAsync(Document, Plate);
        await SaveCustomerWithVehicleAsync("11144477735", "ABC1D23");

        await AssertProblemAsync(
            await admin.PostAsJsonAsync("/api/admin/service-orders", new { customerDocument = "39053344705", licensePlate = Plate, problemDescription = "Barulho" }, CancellationToken),
            HttpStatusCode.NotFound,
            "customer.notFound");
        await AssertProblemAsync(
            await admin.PostAsJsonAsync("/api/admin/service-orders", new { customerDocument = Document, licensePlate = "XYZ9A99", problemDescription = "Barulho" }, CancellationToken),
            HttpStatusCode.NotFound,
            "vehicle.notFound");
        await AssertProblemAsync(
            await admin.PostAsJsonAsync("/api/admin/service-orders", new { customerDocument = Document, licensePlate = "ABC1D23", problemDescription = "Barulho" }, CancellationToken),
            HttpStatusCode.Conflict,
            "serviceOrder.vehicleNotOwnedByCustomer");
        await AssertProblemAsync(
            await admin.PostAsJsonAsync("/api/admin/service-orders", new { customerDocument = "123", licensePlate = "?", problemDescription = "" }, CancellationToken),
            HttpStatusCode.BadRequest,
            "common.validationFailed");
    }

    [Fact]
    public async Task Parts_CrudAndListingFilters_Work()
    {
        using var admin = await ClientAsync(Role.Admin);
        var filter = await SendAsync(admin, HttpMethod.Post, "/api/admin/parts", HttpStatusCode.Created,
            new { code = "FLT-001", name = "Filtro de óleo", type = "Part", unitOfMeasure = "un", unitPrice = 25m, minimumStock = 2m });
        var oil = await SendAsync(admin, HttpMethod.Post, "/api/admin/parts", HttpStatusCode.Created,
            new { code = "OLE-5W30", name = "Óleo 5W30", type = "Supply", unitOfMeasure = "L", unitPrice = 42.5m, minimumStock = 0m });
        var filterId = filter.GetProperty("id").GetGuid();

        await AssertProblemAsync(
            await admin.PostAsJsonAsync("/api/admin/parts", new { code = "flt-001", name = "Outro", type = "Part", unitOfMeasure = "un", unitPrice = 1m, minimumStock = 0m }, CancellationToken),
            HttpStatusCode.Conflict,
            "common.duplicateRecord");

        var updated = await SendAsync(admin, HttpMethod.Put, $"/api/admin/parts/{filterId}", HttpStatusCode.OK,
            new { name = "Filtro de óleo Tecfil", type = "Part", unitOfMeasure = "un", unitPrice = 30m, minimumStock = 1m });
        Assert.Equal(30m, updated.GetProperty("unitPrice").GetDecimal());

        var belowMinimum = await SendAsync(admin, HttpMethod.Get, "/api/admin/parts?belowMinimum=true", HttpStatusCode.OK);
        Assert.Equal(filterId, Assert.Single(belowMinimum.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());

        await SendAsync(admin, HttpMethod.Post, $"/api/admin/parts/{oil.GetProperty("id").GetGuid()}/deactivate", HttpStatusCode.NoContent);
        Assert.Equal(1, (await SendAsync(admin, HttpMethod.Get, "/api/admin/parts", HttpStatusCode.OK)).GetProperty("totalItems").GetInt32());
        Assert.Equal(2, (await SendAsync(admin, HttpMethod.Get, "/api/admin/parts?includeInactive=true", HttpStatusCode.OK)).GetProperty("totalItems").GetInt32());
        await SendAsync(admin, HttpMethod.Post, $"/api/admin/parts/{oil.GetProperty("id").GetGuid()}/reactivate", HttpStatusCode.NoContent);

        await AssertProblemAsync(await admin.GetAsync($"/api/admin/parts/{Guid.CreateVersion7()}", CancellationToken), HttpStatusCode.NotFound, "inventory.notFound");
        await AssertProblemAsync(
            await admin.PostAsJsonAsync($"/api/admin/parts/{filterId}/stock-entries", new { quantity = 0m, reason = "Compra" }, CancellationToken),
            HttpStatusCode.BadRequest,
            "common.validationFailed");
    }

    private async Task<Guid> OpenInDiagnosisWithServiceAsync(HttpClient client)
    {
        await SaveCustomerWithVehicleAsync(Document, Plate);
        var service = await SendAsync(client, HttpMethod.Post, "/api/admin/services", HttpStatusCode.Created,
            new { name = "Alinhamento", basePrice = 120m, estimatedMinutes = 40 });
        var opened = await SendAsync(client, HttpMethod.Post, "/api/admin/service-orders", HttpStatusCode.Created,
            new { customerDocument = Document, licensePlate = Plate, problemDescription = "Carro puxando para a direita" });
        var id = opened.GetProperty("id").GetGuid();
        await SendAsync(client, HttpMethod.Post, $"/api/admin/service-orders/{id}/diagnosis/start", HttpStatusCode.OK);
        await SendAsync(client, HttpMethod.Post, $"/api/admin/service-orders/{id}/services", HttpStatusCode.OK,
            new { serviceId = service.GetProperty("id").GetGuid(), quantity = 1 });
        return id;
    }

    private async Task SaveCustomerWithVehicleAsync(string document, string plate)
    {
        var customer = TestData.Customer(document);
        await using var context = Database.CreateContext(TimeProvider.System);
        context.Customers.Add(customer);
        context.Vehicles.Add(TestData.Vehicle(customer.Id, plate));
        await CommitAsync(context);
    }

    private static async Task<JsonElement> SendAsync(HttpClient client, HttpMethod method, string path, HttpStatusCode expected, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = body is null ? null : JsonContent.Create(body) };
        using var response = await client.SendAsync(request, CancellationToken);
        var content = await response.Content.ReadAsStringAsync(CancellationToken);
        Assert.True(response.StatusCode == expected, $"{method} {path} = {(int)response.StatusCode}: {content}");
        return content.Length == 0 ? default : JsonDocument.Parse(content).RootElement;
    }
}
