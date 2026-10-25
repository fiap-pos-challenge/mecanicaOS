using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.ValueObjects;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class PublicServiceOrdersApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    private const string RawToken = "link-do-orcamento";

    [Fact]
    public async Task ApproveByToken_DeductsStockStartsExecutionAndBurnsTheToken()
    {
        var (order, partId) = await SaveWaitingApprovalAsync(stock: 10m);
        using var client = Factory.CreateClient();

        using var response = await client.PostAsync($"/api/public/quotes/{RawToken}/approve", null, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("InProgress", body.GetProperty("status").GetString());
        Assert.Equal("Approved", body.GetProperty("quote").GetProperty("status").GetString());
        Assert.False(body.TryGetProperty("customer", out _));
        Assert.Equal(8m, await StockOfAsync(partId));
        await using (var reader = Database.CreateContext(TimeProvider.System))
        {
            var stored = await reader.ServiceOrders.Include(item => item.StatusHistory).SingleAsync(item => item.Id == order.Id, CancellationToken);
            Assert.Null(stored.StatusHistory.OrderBy(history => history.ChangedAt).Last().ChangedByUserId);
        }

        await AssertProblemAsync(
            await client.PostAsync($"/api/public/quotes/{RawToken}/approve", null, CancellationToken),
            HttpStatusCode.NotFound,
            "serviceOrder.notFound");
    }

    [Fact]
    public async Task ApproveByToken_InsufficientStock_Returns409AndRollsBack()
    {
        var (order, partId) = await SaveWaitingApprovalAsync(stock: 1m);
        using var client = Factory.CreateClient();

        await AssertProblemAsync(
            await client.PostAsync($"/api/public/quotes/{RawToken}/approve", null, CancellationToken),
            HttpStatusCode.Conflict,
            "inventory.insufficientStock");

        Assert.Equal(1m, await StockOfAsync(partId));
        await using var reader = Database.CreateContext(TimeProvider.System);
        Assert.Equal("WaitingApproval", (await reader.ServiceOrders.SingleAsync(item => item.Id == order.Id, CancellationToken)).Status.ToString());
    }

    [Fact]
    public async Task ApproveByToken_UnknownToken_Returns404()
    {
        using var client = Factory.CreateClient();

        await AssertProblemAsync(
            await client.PostAsync("/api/public/quotes/token-inexistente/approve", null, CancellationToken),
            HttpStatusCode.NotFound,
            "serviceOrder.notFound");
    }

    [Fact]
    public async Task Lookup_NumberAndDocumentMatch_ReturnsStatusItemsAndQuote()
    {
        await SaveWaitingApprovalAsync(stock: 10m);
        using var client = Factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/public/service-orders/lookup", new { number = 1, document = "529.982.247-25" }, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(1, body.GetProperty("number").GetInt64());
        Assert.Equal("WaitingApproval", body.GetProperty("status").GetString());
        Assert.Single(body.GetProperty("services").EnumerateArray());
        Assert.Single(body.GetProperty("parts").EnumerateArray());
        Assert.Equal(150m, body.GetProperty("quote").GetProperty("total").GetDecimal());
        Assert.Equal("BRA2E19", body.GetProperty("vehicle").GetProperty("licensePlate").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("executionStartedAt").ValueKind);
        Assert.False(body.TryGetProperty("customer", out _));
        Assert.False(body.TryGetProperty("statusHistory", out _));
    }

    [Fact]
    public async Task Lookup_DocumentOfAnotherCustomer_Returns404()
    {
        await SaveWaitingApprovalAsync(stock: 10m);
        using var client = Factory.CreateClient();

        await AssertProblemAsync(
            await client.PostAsJsonAsync("/api/public/service-orders/lookup", new { number = 1, document = "11144477735" }, CancellationToken),
            HttpStatusCode.NotFound,
            "serviceOrder.notFound");
    }

    [Fact]
    public async Task Lookup_InvalidDocument_Returns400()
    {
        using var client = Factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/public/service-orders/lookup", new { number = 1, document = "123" }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single((await ReadJsonAsync(response)).GetProperty("errors").EnumerateArray());
        Assert.Equal("customer.invalidDocument", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ApproveInPerson_RecordsTheAttendantAndDeductsStock()
    {
        var (order, partId) = await SaveWaitingApprovalAsync(stock: 10m);
        var attendant = await SaveUserAsync(Role.Attendant);
        using var client = Factory.CreateClient(attendant);

        using var response = await client.PostAsync($"/api/admin/service-orders/{order.Id}/quote/approve", null, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("InProgress", body.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, body.GetProperty("executionStartedAt").ValueKind);
        var last = body.GetProperty("statusHistory").EnumerateArray().Last();
        Assert.Equal(attendant.Id, last.GetProperty("changedByUserId").GetGuid());
        Assert.Equal(8m, await StockOfAsync(partId));
    }

    [Fact]
    public async Task ApproveInPerson_AlreadyApproved_Returns409()
    {
        var (order, _) = await SaveWaitingApprovalAsync(stock: 10m);
        using var client = await ClientAsync(Role.Admin);
        await client.PostAsync($"/api/admin/service-orders/{order.Id}/quote/approve", null, CancellationToken);

        await AssertProblemAsync(
            await client.PostAsync($"/api/admin/service-orders/{order.Id}/quote/approve", null, CancellationToken),
            HttpStatusCode.Conflict,
            "serviceOrder.invalidTransition");
    }

    [Fact]
    public async Task ApproveInPerson_UnknownServiceOrder_Returns404()
    {
        using var client = await ClientAsync(Role.Admin);

        await AssertProblemAsync(
            await client.PostAsync($"/api/admin/service-orders/{Guid.CreateVersion7()}/quote/approve", null, CancellationToken),
            HttpStatusCode.NotFound,
            "serviceOrder.notFound");
    }

    private async Task<(ServiceOrder Order, Guid PartId)> SaveWaitingApprovalAsync(decimal stock)
    {
        var customer = TestData.Customer();
        var vehicle = TestData.Vehicle(customer.Id);
        var service = TestData.Service(100m);
        var part = TestData.Part(stock: 10m);
        var order = TestData.ServiceOrder(customer.Id, vehicle.Id, 1);
        order.StartDiagnosis(null, TestData.Today.AddMinutes(10));
        order.AddServiceItem(service, 1);
        order.AddPartItem(part, Quantity.Create(2m).Value);
        order.SendQuote(ApprovalToken.FromRawToken(RawToken).Value, null, TestData.Today.AddMinutes(30));

        await using (var context = Database.CreateContext(TimeProvider.System))
        {
            context.Customers.Add(customer);
            context.Vehicles.Add(vehicle);
            context.Services.Add(service);
            context.Parts.Add(part);
            context.ServiceOrders.Add(order);
            await CommitAsync(context);
        }

        if (stock < 10m)
        {
            await using var context = Database.CreateContext(TimeProvider.System);
            await context.Database.ExecuteSqlAsync($"UPDATE [parts] SET [stockQuantity] = {stock} WHERE [id] = {part.Id}", CancellationToken);
        }

        return (order, part.Id);
    }

    private async Task<decimal> StockOfAsync(Guid partId)
    {
        await using var context = Database.CreateContext(TimeProvider.System);
        return (await context.Parts.AsNoTracking().SingleAsync(part => part.Id == partId, CancellationToken)).StockQuantity;
    }
}
