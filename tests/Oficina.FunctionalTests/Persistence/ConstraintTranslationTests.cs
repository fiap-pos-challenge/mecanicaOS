using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.FunctionalTests.Fixtures;
using Oficina.Infrastructure.Messaging.Outbox;
using Oficina.Infrastructure.Persistence;

namespace Oficina.FunctionalTests.Persistence;

[Collection(nameof(DatabaseCollection))]
public sealed class ConstraintTranslationTests(SqlServerFixture database) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await database.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task DuplicateDocument_ReturnsDuplicateRecord()
    {
        Assert.True((await CommitAsync(context => context.Customers.Add(TestData.Customer()))).IsSuccess);

        var result = await CommitAsync(context => context.Customers.Add(TestData.Customer()));

        Assert.Equal(CommonErrors.DuplicateRecord, result.Error);
    }

    [Fact]
    public async Task DocumentOfDeletedCustomer_CanBeReused()
    {
        var first = TestData.Customer();
        await CommitAsync(context => context.Customers.Add(first));
        await CommitAsync(async context => context.Customers.Remove(await context.Customers.SingleAsync(c => c.Id == first.Id, TestContext.Current.CancellationToken)));

        var result = await CommitAsync(context => context.Customers.Add(TestData.Customer()));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DuplicateLicensePlate_ReturnsDuplicateRecord_AndIsReusableAfterDeletion()
    {
        var customer = TestData.Customer();
        var first = TestData.Vehicle(customer.Id);
        await CommitAsync(context =>
        {
            context.Customers.Add(customer);
            context.Vehicles.Add(first);
        });

        Assert.Equal(CommonErrors.DuplicateRecord, (await CommitAsync(context => context.Vehicles.Add(TestData.Vehicle(customer.Id)))).Error);

        await CommitAsync(async context => context.Vehicles.Remove(await context.Vehicles.SingleAsync(v => v.Id == first.Id, TestContext.Current.CancellationToken)));
        Assert.True((await CommitAsync(context => context.Vehicles.Add(TestData.Vehicle(customer.Id)))).IsSuccess);
    }

    [Fact]
    public async Task DuplicateUserEmail_ReturnsDuplicateRecord_AndIsReusableAfterDeletion()
    {
        var first = TestData.User();
        await CommitAsync(context => context.Users.Add(first));

        Assert.Equal(CommonErrors.DuplicateRecord, (await CommitAsync(context => context.Users.Add(TestData.User()))).Error);

        await CommitAsync(async context => context.Users.Remove(await context.Users.SingleAsync(u => u.Id == first.Id, TestContext.Current.CancellationToken)));
        Assert.True((await CommitAsync(context => context.Users.Add(TestData.User()))).IsSuccess);
    }

    [Fact]
    public async Task DuplicatePartCode_ReturnsDuplicateRecord_AndIsReusableAfterDeletion()
    {
        var first = TestData.Part();
        await CommitAsync(context => context.Parts.Add(first));

        Assert.Equal(CommonErrors.DuplicateRecord, (await CommitAsync(context => context.Parts.Add(TestData.Part()))).Error);

        await CommitAsync(async context => context.Parts.Remove(await context.Parts.SingleAsync(p => p.Id == first.Id, TestContext.Current.CancellationToken)));
        Assert.True((await CommitAsync(context => context.Parts.Add(TestData.Part()))).IsSuccess);
    }

    [Fact]
    public async Task SecondOpenOrderForTheSameVehicle_ReturnsDuplicateRecord()
    {
        var customer = TestData.Customer();
        var vehicle = TestData.Vehicle(customer.Id);
        await CommitAsync(context =>
        {
            context.Customers.Add(customer);
            context.Vehicles.Add(vehicle);
            context.ServiceOrders.Add(TestData.ServiceOrder(customer.Id, vehicle.Id, 7001));
        });

        var result = await CommitAsync(context => context.ServiceOrders.Add(TestData.ServiceOrder(customer.Id, vehicle.Id, 7002)));

        Assert.Equal(CommonErrors.DuplicateRecord, result.Error);
    }

    [Fact]
    public async Task NewOrderForVehicle_IsAllowedAfterThePreviousWasDelivered()
    {
        var customer = TestData.Customer();
        var vehicle = TestData.Vehicle(customer.Id);
        var service = TestData.Service();
        var delivered = TestData.ServiceOrder(customer.Id, vehicle.Id, 7101);
        delivered.StartDiagnosis(null, TestData.Today);
        delivered.AddServiceItem(service, 1);
        delivered.SendQuote(TestData.Token(), null, TestData.Today);
        delivered.ApproveQuote(null, TestData.Today);
        delivered.Complete(null, TestData.Today);
        delivered.Deliver(null, TestData.Today);
        await CommitAsync(context =>
        {
            context.Customers.Add(customer);
            context.Vehicles.Add(vehicle);
            context.Services.Add(service);
            context.ServiceOrders.Add(delivered);
        });

        var result = await CommitAsync(context => context.ServiceOrders.Add(TestData.ServiceOrder(customer.Id, vehicle.Id, 7102)));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ConcurrentUpdatesOfAPart_SecondReturnsConcurrencyConflict()
    {
        var part = TestData.Part();
        await CommitAsync(context => context.Parts.Add(part));
        await using var first = database.CreateContext(TimeProvider.System);
        await using var second = database.CreateContext(TimeProvider.System);
        var firstCopy = await first.Parts.SingleAsync(p => p.Id == part.Id, TestContext.Current.CancellationToken);
        var secondCopy = await second.Parts.SingleAsync(p => p.Id == part.Id, TestContext.Current.CancellationToken);

        firstCopy.RegisterEntry(Quantity.Create(1m).Value, "Compra A", null);
        secondCopy.RegisterEntry(Quantity.Create(2m).Value, "Compra B", null);
        var firstResult = await new UnitOfWork(first, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken);
        var secondResult = await new UnitOfWork(second, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken);

        Assert.True(firstResult.IsSuccess);
        Assert.Equal(CommonErrors.ConcurrencyConflict, secondResult.Error);
    }

    [Fact]
    public async Task ConcurrentUpdatesOfAServiceOrder_SecondReturnsConcurrencyConflict()
    {
        var customer = TestData.Customer();
        var vehicle = TestData.Vehicle(customer.Id);
        var order = TestData.ServiceOrder(customer.Id, vehicle.Id, 7201);
        await CommitAsync(context =>
        {
            context.Customers.Add(customer);
            context.Vehicles.Add(vehicle);
            context.ServiceOrders.Add(order);
        });
        await using var first = database.CreateContext(TimeProvider.System);
        await using var second = database.CreateContext(TimeProvider.System);
        var firstCopy = await first.ServiceOrders.SingleAsync(o => o.Id == order.Id, TestContext.Current.CancellationToken);
        var secondCopy = await second.ServiceOrders.SingleAsync(o => o.Id == order.Id, TestContext.Current.CancellationToken);

        firstCopy.StartDiagnosis(null, TestData.Today);
        secondCopy.StartDiagnosis(null, TestData.Today);
        var firstResult = await new UnitOfWork(first, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken);
        var secondResult = await new UnitOfWork(second, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken);

        Assert.True(firstResult.IsSuccess);
        Assert.Equal(CommonErrors.ConcurrencyConflict, secondResult.Error);
    }

    [Fact]
    public async Task ForeignKeyViolation_IsNotTranslated_AndPropagates()
    {
        await using var context = database.CreateContext(TimeProvider.System);
        context.Vehicles.Add(TestData.Vehicle(Guid.CreateVersion7()));

        await Assert.ThrowsAsync<DbUpdateException>(() => new UnitOfWork(context, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken));
    }

    private Task<Result> CommitAsync(Action<OficinaDbContext> change) =>
        CommitAsync(context =>
        {
            change(context);
            return Task.CompletedTask;
        });

    private async Task<Result> CommitAsync(Func<OficinaDbContext, Task> change)
    {
        await using var context = database.CreateContext(TimeProvider.System);
        await change(context);
        return await new UnitOfWork(context, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken);
    }
}
