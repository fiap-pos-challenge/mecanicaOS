using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class ServiceOrdersApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    [Fact]
    public async Task List_ReturnsNewestFirstWithPlateCustomerAndTotal()
    {
        var (customer, vehicle) = await SaveCustomerWithVehicleAsync("52998224725", "BRA2E19");
        var (_, otherVehicle) = await SaveCustomerWithVehicleAsync("11144477735", "ABC1D23");
        await SaveDeliveredAsync(customer.Id, vehicle.Id, 1);
        await SaveAsync(TestData.ServiceOrder(customer.Id, otherVehicle.Id, 2));
        using var client = await ClientAsync(Role.Mechanic);

        var page = await ReadJsonAsync(await client.GetAsync("/api/admin/service-orders", CancellationToken));

        var items = page.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(2, page.GetProperty("totalItems").GetInt32());
        Assert.Equal(20, page.GetProperty("pageSize").GetInt32());
        Assert.Equal(new long[] { 2, 1 }, items.Select(item => item.GetProperty("number").GetInt64()).ToArray());
        Assert.Equal("ABC1D23", items[0].GetProperty("licensePlate").GetString());
        Assert.Equal("Maria Souza", items[1].GetProperty("customerName").GetString());
        Assert.Equal("Delivered", items[1].GetProperty("status").GetString());
        Assert.Equal(100m, items[1].GetProperty("totalAmount").GetDecimal());
        Assert.NotEqual(default, items[1].GetProperty("openedAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task List_FiltersByStatusAndPlateCombined()
    {
        var (customer, vehicle) = await SaveCustomerWithVehicleAsync("52998224725", "BRA2E19");
        var (_, otherVehicle) = await SaveCustomerWithVehicleAsync("11144477735", "ABC1D23");
        await SaveDeliveredAsync(customer.Id, vehicle.Id, 1);
        await SaveAsync(TestData.ServiceOrder(customer.Id, vehicle.Id, 2));
        await SaveAsync(TestData.ServiceOrder(customer.Id, otherVehicle.Id, 3));
        using var client = await ClientAsync(Role.Attendant);

        var byStatus = await NumbersAsync(client, "/api/admin/service-orders?status=Received");
        var byPlate = await NumbersAsync(client, "/api/admin/service-orders?plate=BRA2E19");
        var combined = await NumbersAsync(client, "/api/admin/service-orders?status=Received&plate=BRA2E19");
        var none = await NumbersAsync(client, "/api/admin/service-orders?status=InProgress");

        Assert.Equal(new long[] { 3, 2 }, byStatus);
        Assert.Equal(new long[] { 2, 1 }, byPlate);
        Assert.Equal(new long[] { 2 }, combined);
        Assert.Empty(none);
    }

    [Fact]
    public async Task List_UnknownStatus_Returns400()
    {
        using var client = await ClientAsync(Role.Admin);

        using var response = await client.GetAsync("/api/admin/service-orders?status=Cancelled", CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single((await ReadJsonAsync(response)).GetProperty("errors").EnumerateArray());
        Assert.Equal("serviceOrder.invalidStatus", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetById_ReturnsTheFullDetailEvenAfterCatalogAndRegistrationChanges()
    {
        var mechanic = await SaveUserAsync(Role.Mechanic);
        var (customer, vehicle) = await SaveCustomerWithVehicleAsync("52998224725", "BRA2E19");
        var service = TestData.Service(150m);
        var part = TestData.Part();
        var order = TestData.ServiceOrder(customer.Id, vehicle.Id, 1);
        order.StartDiagnosis(mechanic.Id, TestData.Today.AddMinutes(10));
        order.AddServiceItem(service, 1);
        order.AddPartItem(part, Quantity.Create(2m).Value);
        order.SendQuote(TestData.Token(), null, TestData.Today.AddMinutes(30));
        order.ApproveQuote(null, TestData.Today.AddHours(1));
        order.Complete(mechanic.Id, TestData.Today.AddHours(3));
        order.Deliver(null, TestData.Today.AddHours(4));
        await using (var context = Database.CreateContext(TimeProvider.System))
        {
            context.Services.Add(service);
            context.Parts.Add(part);
            context.ServiceOrders.Add(order);
            await CommitAsync(context);
        }

        await using (var context = Database.CreateContext(TimeProvider.System))
        {
            var storedService = await context.Services.SingleAsync(item => item.Id == service.Id, CancellationToken);
            storedService.Deactivate();
            context.Vehicles.Remove(await context.Vehicles.SingleAsync(item => item.Id == vehicle.Id, CancellationToken));
            context.Customers.Remove(await context.Customers.SingleAsync(item => item.Id == customer.Id, CancellationToken));
            await CommitAsync(context);
        }

        using var client = await ClientAsync(Role.Attendant);
        var detail = await ReadJsonAsync(await client.GetAsync($"/api/admin/service-orders/{order.Id}", CancellationToken));

        Assert.Equal(1, detail.GetProperty("number").GetInt64());
        Assert.Equal("Delivered", detail.GetProperty("status").GetString());
        Assert.Equal("Barulho ao frear", detail.GetProperty("problemDescription").GetString());
        Assert.Equal("Maria Souza", detail.GetProperty("customer").GetProperty("name").GetString());
        Assert.Equal("52998224725", detail.GetProperty("customer").GetProperty("document").GetString());
        Assert.Equal("maria@exemplo.com", detail.GetProperty("customer").GetProperty("email").GetString());
        Assert.Equal("BRA2E19", detail.GetProperty("vehicle").GetProperty("licensePlate").GetString());
        Assert.Equal(2020, detail.GetProperty("vehicle").GetProperty("year").GetInt32());

        var serviceItem = Assert.Single(detail.GetProperty("services").EnumerateArray());
        Assert.Equal("Troca de óleo", serviceItem.GetProperty("name").GetString());
        Assert.Equal(150m, serviceItem.GetProperty("unitPrice").GetDecimal());

        var partItem = Assert.Single(detail.GetProperty("parts").EnumerateArray());
        Assert.Equal("Filtro de óleo", partItem.GetProperty("name").GetString());
        Assert.Equal(2m, partItem.GetProperty("quantity").GetDecimal());
        Assert.Equal(25m, partItem.GetProperty("unitPrice").GetDecimal());
        Assert.Equal(50m, partItem.GetProperty("total").GetDecimal());

        var quote = detail.GetProperty("quote");
        Assert.Equal(150m, quote.GetProperty("servicesTotal").GetDecimal());
        Assert.Equal(50m, quote.GetProperty("partsTotal").GetDecimal());
        Assert.Equal(200m, quote.GetProperty("total").GetDecimal());
        Assert.Equal("Approved", quote.GetProperty("status").GetString());
        Assert.False(quote.TryGetProperty("approvalToken", out _));

        var history = detail.GetProperty("statusHistory").EnumerateArray().ToArray();
        Assert.Equal(6, history.Length);
        Assert.Equal(JsonValueKind.Null, history[0].GetProperty("previousStatus").ValueKind);
        Assert.Equal("Received", history[1].GetProperty("previousStatus").GetString());
        Assert.Equal("InDiagnosis", history[1].GetProperty("newStatus").GetString());
        Assert.Equal(mechanic.Id, history[1].GetProperty("changedByUserId").GetGuid());
        Assert.Equal(mechanic.Name, history[1].GetProperty("changedByName").GetString());

        Assert.Equal(TestData.Today.AddHours(1), detail.GetProperty("executionStartedAt").GetDateTimeOffset());
        Assert.Equal(TestData.Today.AddHours(3), detail.GetProperty("completedAt").GetDateTimeOffset());
        Assert.Equal(TestData.Today.AddHours(4), detail.GetProperty("deliveredAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task GetById_WithoutQuote_ReturnsNullQuoteAndNullDates()
    {
        var (customer, vehicle) = await SaveCustomerWithVehicleAsync("52998224725", "BRA2E19");
        var order = TestData.ServiceOrder(customer.Id, vehicle.Id, 1);
        await SaveAsync(order);
        using var client = await ClientAsync(Role.Mechanic);

        var detail = await ReadJsonAsync(await client.GetAsync($"/api/admin/service-orders/{order.Id}", CancellationToken));

        Assert.Equal(JsonValueKind.Null, detail.GetProperty("quote").ValueKind);
        Assert.Empty(detail.GetProperty("services").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("executionStartedAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("completedAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("deliveredAt").ValueKind);
    }

    [Fact]
    public async Task GetById_UnknownServiceOrder_Returns404()
    {
        using var client = await ClientAsync(Role.Admin);

        await AssertProblemAsync(
            await client.GetAsync($"/api/admin/service-orders/{Guid.CreateVersion7()}", CancellationToken),
            HttpStatusCode.NotFound,
            "serviceOrder.notFound");
    }

    [Fact]
    public async Task AverageExecutionTime_AveragesCompletedAndDeliveredOrdersInThePeriod()
    {
        var (customer, vehicle) = await SaveCustomerWithVehicleAsync("52998224725", "BRA2E19");
        var (_, secondVehicle) = await SaveCustomerWithVehicleAsync("11144477735", "ABC1D23");
        var (_, thirdVehicle) = await SaveCustomerWithVehicleAsync("39053344705", "XYZ9876");
        await SaveDeliveredAsync(customer.Id, vehicle.Id, 1);
        await SaveCompletedAsync(customer.Id, secondVehicle.Id, 2, TimeSpan.FromHours(4));
        await SaveAsync(TestData.ServiceOrder(customer.Id, thirdVehicle.Id, 3));
        using var client = await ClientAsync(Role.Admin);

        var all = await ReadJsonAsync(await client.GetAsync("/api/admin/metrics/average-execution-time", CancellationToken));
        var sameDay = await ReadJsonAsync(await client.GetAsync("/api/admin/metrics/average-execution-time?from=2026-09-30&to=2026-09-30", CancellationToken));
        var empty = await ReadJsonAsync(await client.GetAsync("/api/admin/metrics/average-execution-time?from=2026-10-01", CancellationToken));

        Assert.Equal(180, all.GetProperty("averageMinutes").GetInt64());
        Assert.Equal(2, all.GetProperty("totalServiceOrders").GetInt32());
        Assert.Equal(2, sameDay.GetProperty("totalServiceOrders").GetInt32());
        Assert.Equal(JsonValueKind.Null, empty.GetProperty("averageMinutes").ValueKind);
        Assert.Equal(0, empty.GetProperty("totalServiceOrders").GetInt32());
    }

    [Fact]
    public async Task AverageExecutionTime_FromAfterTo_Returns400()
    {
        using var client = await ClientAsync(Role.Admin);

        using var response = await client.GetAsync("/api/admin/metrics/average-execution-time?from=2026-10-02&to=2026-10-01", CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single((await ReadJsonAsync(response)).GetProperty("errors").EnumerateArray());
        Assert.Equal("serviceOrder.invalidPeriod", error.GetProperty("code").GetString());
    }

    private async Task SaveDeliveredAsync(Guid customerId, Guid vehicleId, long number)
    {
        var service = TestData.Service();
        var order = Completed(customerId, vehicleId, number, service, TimeSpan.FromHours(2));
        order.Deliver(null, TestData.Today.AddHours(5));
        await SaveAsync(order, service);
    }

    private async Task SaveCompletedAsync(Guid customerId, Guid vehicleId, long number, TimeSpan executionTime)
    {
        var service = TestData.Service();
        await SaveAsync(Completed(customerId, vehicleId, number, service, executionTime), service);
    }

    private static ServiceOrder Completed(Guid customerId, Guid vehicleId, long number, Service service, TimeSpan executionTime)
    {
        var order = TestData.ServiceOrder(customerId, vehicleId, number);
        order.StartDiagnosis(null, TestData.Today.AddMinutes(10));
        order.AddServiceItem(service, 1);
        order.SendQuote(TestData.Token(), null, TestData.Today.AddMinutes(30));
        order.ApproveQuote(null, TestData.Today.AddHours(1));
        order.Complete(null, TestData.Today.AddHours(1).Add(executionTime));
        return order;
    }

    private async Task<(Customer Customer, Vehicle Vehicle)> SaveCustomerWithVehicleAsync(string document, string plate)
    {
        var customer = TestData.Customer(document);
        var vehicle = TestData.Vehicle(customer.Id, plate);
        await using var context = Database.CreateContext(TimeProvider.System);
        context.Customers.Add(customer);
        context.Vehicles.Add(vehicle);
        await CommitAsync(context);
        return (customer, vehicle);
    }

    private async Task SaveAsync(ServiceOrder order, Service? service = null)
    {
        await using var context = Database.CreateContext(TimeProvider.System);
        if (service is not null)
        {
            context.Services.Add(service);
        }

        context.ServiceOrders.Add(order);
        await CommitAsync(context);
    }

    private static async Task<long[]> NumbersAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("items").EnumerateArray().Select(item => item.GetProperty("number").GetInt64()).ToArray();
    }
}
