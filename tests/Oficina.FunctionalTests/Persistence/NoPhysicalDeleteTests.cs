using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.FunctionalTests.Fixtures;
using Oficina.Infrastructure.Messaging.Outbox;
using Oficina.Infrastructure.Persistence;

namespace Oficina.FunctionalTests.Persistence;

[Collection(nameof(DatabaseCollection))]
public sealed class NoPhysicalDeleteTests(SqlServerFixture database) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await database.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task User_Remove_KeepsTheRow()
    {
        var user = TestData.User();
        await SaveAsync(context => context.Users.Add(user));

        await SaveAsync(async context => context.Users.Remove(await context.Users.SingleAsync(u => u.Id == user.Id, TestContext.Current.CancellationToken)));

        await AssertSoftDeletedAsync("users", user.Id);
    }

    [Fact]
    public async Task Customer_Remove_KeepsTheRow()
    {
        var customer = TestData.Customer();
        await SaveAsync(context => context.Customers.Add(customer));

        await SaveAsync(async context => context.Customers.Remove(await context.Customers.SingleAsync(c => c.Id == customer.Id, TestContext.Current.CancellationToken)));

        await AssertSoftDeletedAsync("customers", customer.Id);
    }

    [Fact]
    public async Task Vehicle_Remove_KeepsTheRow()
    {
        var customer = TestData.Customer();
        var vehicle = TestData.Vehicle(customer.Id);
        await SaveAsync(context =>
        {
            context.Customers.Add(customer);
            context.Vehicles.Add(vehicle);
        });

        await SaveAsync(async context => context.Vehicles.Remove(await context.Vehicles.SingleAsync(v => v.Id == vehicle.Id, TestContext.Current.CancellationToken)));

        await AssertSoftDeletedAsync("vehicles", vehicle.Id);
    }

    [Fact]
    public async Task Service_Remove_KeepsTheRow()
    {
        var service = TestData.Service();
        await SaveAsync(context => context.Services.Add(service));

        await SaveAsync(async context => context.Services.Remove(await context.Services.SingleAsync(s => s.Id == service.Id, TestContext.Current.CancellationToken)));

        await AssertSoftDeletedAsync("services", service.Id);
    }

    [Fact]
    public async Task Part_Remove_KeepsThePartAndItsMovements()
    {
        var part = TestData.Part();
        await SaveAsync(context => context.Parts.Add(part));
        var movementId = part.StockMovements.Single().Id;

        await SaveAsync(async context => context.Parts.Remove(
            await context.Parts.Include(p => p.StockMovements).SingleAsync(p => p.Id == part.Id, TestContext.Current.CancellationToken)));

        await AssertSoftDeletedAsync("parts", part.Id);
        await AssertSoftDeletedAsync("stockMovements", movementId);
    }

    [Fact]
    public async Task ServiceOrder_Remove_KeepsTheOrderAndItsHistory()
    {
        var customer = TestData.Customer();
        var vehicle = TestData.Vehicle(customer.Id);
        var order = TestData.ServiceOrder(customer.Id, vehicle.Id, 9001);
        await SaveAsync(context =>
        {
            context.Customers.Add(customer);
            context.Vehicles.Add(vehicle);
            context.ServiceOrders.Add(order);
        });
        var historyId = order.StatusHistory.Single().Id;

        await SaveAsync(async context => context.ServiceOrders.Remove(
            await context.ServiceOrders.Include(o => o.StatusHistory).SingleAsync(o => o.Id == order.Id, TestContext.Current.CancellationToken)));

        await AssertSoftDeletedAsync("serviceOrders", order.Id);
        await AssertSoftDeletedAsync("serviceOrderStatusHistory", historyId);
    }

    [Fact]
    public async Task ServiceOrder_RemoveItem_SoftDeletesTheItem()
    {
        var customer = TestData.Customer();
        var vehicle = TestData.Vehicle(customer.Id);
        var service = TestData.Service();
        var part = TestData.Part();
        var order = TestData.ServiceOrder(customer.Id, vehicle.Id, 9002);
        order.StartDiagnosis(null, TestData.Today);
        order.AddServiceItem(service, 1);
        order.AddPartItem(part, Quantity.Create(1m).Value);
        await SaveAsync(context =>
        {
            context.Customers.Add(customer);
            context.Vehicles.Add(vehicle);
            context.Services.Add(service);
            context.Parts.Add(part);
            context.ServiceOrders.Add(order);
        });
        var serviceItemId = order.ServiceItems.Single().Id;
        var partItemId = order.PartItems.Single().Id;

        await SaveAsync(async context =>
        {
            var loaded = await context.ServiceOrders
                .Include(o => o.ServiceItems)
                .Include(o => o.PartItems)
                .SingleAsync(o => o.Id == order.Id, TestContext.Current.CancellationToken);
            Assert.Equal(ServiceOrderStatus.InDiagnosis, loaded.Status);
            Assert.True(loaded.RemoveItem(serviceItemId).IsSuccess);
            Assert.True(loaded.RemoveItem(partItemId).IsSuccess);
        });

        await AssertSoftDeletedAsync("serviceItems", serviceItemId);
        await AssertSoftDeletedAsync("partItems", partItemId);
        await using var reader = database.CreateContext(TimeProvider.System);
        var reloaded = await reader.ServiceOrders.Include(o => o.ServiceItems).Include(o => o.PartItems)
            .SingleAsync(o => o.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Empty(reloaded.ServiceItems);
        Assert.Empty(reloaded.PartItems);
    }

    private async Task SaveAsync(Action<OficinaDbContext> change) =>
        await SaveAsync(context =>
        {
            change(context);
            return Task.CompletedTask;
        });

    private async Task SaveAsync(Func<OficinaDbContext, Task> change)
    {
        await using var context = database.CreateContext(TimeProvider.System);
        await change(context);
        var result = await new UnitOfWork(context, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
    }

    private async Task AssertSoftDeletedAsync(string table, Guid id)
    {
        Assert.Equal(1, await database.CountRowsAsync(table, id));
        var rows = await database.QueryAsync($"SELECT deletedAt FROM [{table}] WHERE id = '{id}'");
        Assert.NotNull(Assert.Single(rows)["deletedAt"]);
    }
}
