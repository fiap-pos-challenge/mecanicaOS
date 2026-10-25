using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.FunctionalTests.Fixtures;
using Oficina.Infrastructure.Messaging.Outbox;
using Oficina.Infrastructure.Persistence;

namespace Oficina.FunctionalTests.Persistence;

[Collection(nameof(DatabaseCollection))]
public sealed class AuditingInterceptorTests(SqlServerFixture database) : IAsyncLifetime
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _clock = new(CreatedAt);

    public async ValueTask InitializeAsync() => await database.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Insert_SetsOnlyCreatedAt()
    {
        var customer = TestData.Customer();
        await using (var context = database.CreateContext(_clock))
        {
            context.Customers.Add(customer);
            await new UnitOfWork(context, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken);
        }

        await using var reader = database.CreateContext(_clock);
        var stored = await reader.Customers.SingleAsync(c => c.Id == customer.Id, TestContext.Current.CancellationToken);
        Assert.Equal(CreatedAt, stored.CreatedAt);
        Assert.Null(stored.UpdatedAt);
        Assert.Null(stored.DeletedAt);
    }

    [Fact]
    public async Task Update_SetsUpdatedAt_AndKeepsCreatedAt()
    {
        var customer = TestData.Customer();
        await using (var context = database.CreateContext(_clock))
        {
            context.Customers.Add(customer);
            await new UnitOfWork(context, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken);
        }

        var updatedAt = CreatedAt.AddHours(5);
        _clock.SetUtcNow(updatedAt);
        await using (var context = database.CreateContext(_clock))
        {
            var loaded = await context.Customers.SingleAsync(c => c.Id == customer.Id, TestContext.Current.CancellationToken);
            loaded.ChangeContact("Maria Lima", null, Phone.Create("2134567890").Value);
            await new UnitOfWork(context, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken);
        }

        await using var reader = database.CreateContext(_clock);
        var stored = await reader.Customers.SingleAsync(c => c.Id == customer.Id, TestContext.Current.CancellationToken);
        Assert.Equal(CreatedAt, stored.CreatedAt);
        Assert.Equal(updatedAt, stored.UpdatedAt);
        Assert.Null(stored.DeletedAt);
    }

    [Fact]
    public async Task Remove_SetsOnlyDeletedAt_KeepsTheRow_AndHidesItFromQueries()
    {
        var customer = TestData.Customer();
        await using (var context = database.CreateContext(_clock))
        {
            context.Customers.Add(customer);
            await new UnitOfWork(context, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken);
        }

        var deletedAt = CreatedAt.AddDays(1);
        _clock.SetUtcNow(deletedAt);
        await using (var context = database.CreateContext(_clock))
        {
            var loaded = await context.Customers.SingleAsync(c => c.Id == customer.Id, TestContext.Current.CancellationToken);
            context.Customers.Remove(loaded);
            await new UnitOfWork(context, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken);
        }

        await using var reader = database.CreateContext(_clock);
        Assert.False(await reader.Customers.AnyAsync(c => c.Id == customer.Id, TestContext.Current.CancellationToken));
        var stored = await reader.Customers.IgnoreQueryFilters().SingleAsync(c => c.Id == customer.Id, TestContext.Current.CancellationToken);
        Assert.Equal(deletedAt, stored.DeletedAt);
        Assert.Null(stored.UpdatedAt);
        Assert.Equal(1, await database.CountRowsAsync("customers", customer.Id));
    }

    [Fact]
    public async Task ChangingOnlyTheAddress_SetsUpdatedAtOnTheCustomer()
    {
        var customer = TestData.Customer();
        await using (var context = database.CreateContext(_clock))
        {
            context.Customers.Add(customer);
            await new UnitOfWork(context, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken);
        }

        var updatedAt = CreatedAt.AddHours(2);
        _clock.SetUtcNow(updatedAt);
        await using (var context = database.CreateContext(_clock))
        {
            var loaded = await context.Customers.SingleAsync(c => c.Id == customer.Id, TestContext.Current.CancellationToken);
            loaded.ChangeAddress(TestData.FullAddress());
            await new UnitOfWork(context, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken);
        }

        await using var reader = database.CreateContext(_clock);
        var stored = await reader.Customers.SingleAsync(c => c.Id == customer.Id, TestContext.Current.CancellationToken);
        Assert.Equal(TestData.FullAddress(), stored.Address);
        Assert.Equal(updatedAt, stored.UpdatedAt);
    }

    [Fact]
    public async Task SynchronousSave_AlsoAuditsAndSoftDeletes()
    {
        var service = TestData.Service();
        await using (var context = database.CreateContext(_clock))
        {
            context.Services.Add(service);
            context.SaveChanges();
        }

        _clock.SetUtcNow(CreatedAt.AddMinutes(30));
        await using (var context = database.CreateContext(_clock))
        {
            context.Services.Remove(context.Services.Single(s => s.Id == service.Id));
            context.SaveChanges();
        }

        await using var reader = database.CreateContext(_clock);
        var stored = await reader.Services.IgnoreQueryFilters().SingleAsync(s => s.Id == service.Id, TestContext.Current.CancellationToken);
        Assert.Equal(CreatedAt, stored.CreatedAt);
        Assert.Equal(CreatedAt.AddMinutes(30), stored.DeletedAt);
    }

    [Fact]
    public async Task Remove_CustomerWithAddress_KeepsTheAddressColumns()
    {
        var customer = TestData.Customer(address: TestData.FullAddress());
        await using (var context = database.CreateContext(_clock))
        {
            context.Customers.Add(customer);
            await new UnitOfWork(context, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = database.CreateContext(_clock))
        {
            var loaded = await context.Customers.SingleAsync(c => c.Id == customer.Id, TestContext.Current.CancellationToken);
            context.Customers.Remove(loaded);
            await new UnitOfWork(context, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken);
        }

        await using var reader = database.CreateContext(_clock);
        var stored = await reader.Customers.IgnoreQueryFilters().SingleAsync(c => c.Id == customer.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(stored.DeletedAt);
        Assert.Equal(TestData.FullAddress(), stored.Address);
    }
}
