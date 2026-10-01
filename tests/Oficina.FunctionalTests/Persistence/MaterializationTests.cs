using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Enums;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.Events;
using Oficina.FunctionalTests.Fixtures;
using Oficina.Infrastructure.Messaging.Outbox;
using Oficina.Infrastructure.Persistence;

namespace Oficina.FunctionalTests.Persistence;

[Collection(nameof(DatabaseCollection))]
public sealed class MaterializationTests(SqlServerFixture database) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await database.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Customer_WithoutAddress_ComesBackWithNullAddress()
    {
        var customer = TestData.Customer();
        await SaveAsync(context => context.Customers.Add(customer));

        await using var reader = database.CreateContext(TimeProvider.System);
        var loaded = await reader.Customers.SingleAsync(c => c.Id == customer.Id, TestContext.Current.CancellationToken);

        Assert.Null(loaded.Address);
        Assert.Equal(customer.Document, loaded.Document);
        Assert.Equal(DocumentType.Cpf, loaded.DocumentType);
        Assert.Equal(customer.Email, loaded.Email);
        Assert.Equal(customer.Phone, loaded.Phone);
        Assert.Empty(loaded.DomainEvents);
    }

    [Fact]
    public async Task Customer_WithAddress_ComesBackWithTheCompleteAddress()
    {
        var customer = TestData.Customer(address: TestData.FullAddress());
        await SaveAsync(context => context.Customers.Add(customer));

        await using var reader = database.CreateContext(TimeProvider.System);
        var loaded = await reader.Customers.SingleAsync(c => c.Id == customer.Id, TestContext.Current.CancellationToken);

        Assert.Equal(TestData.FullAddress(), loaded.Address);
    }

    [Fact]
    public async Task Customer_WithoutEmail_ComesBackWithNullEmail()
    {
        var customer = Domain.Customers.Entities.Customer.Create(
            "João Silva",
            Domain.Customers.ValueObjects.Document.Create("11222333000181").Value,
            null,
            Domain.Customers.ValueObjects.Phone.Create("2134567890").Value,
            null).Value;
        await SaveAsync(context => context.Customers.Add(customer));

        await using var reader = database.CreateContext(TimeProvider.System);
        var loaded = await reader.Customers.SingleAsync(c => c.Id == customer.Id, TestContext.Current.CancellationToken);

        Assert.Null(loaded.Email);
        Assert.Equal(DocumentType.Cnpj, loaded.DocumentType);
    }

    [Fact]
    public async Task Vehicle_ComesBackWithYearWithoutRevalidatingAgainstToday()
    {
        var customer = TestData.Customer();
        var vehicle = TestData.Vehicle(customer.Id);
        await SaveAsync(context =>
        {
            context.Customers.Add(customer);
            context.Vehicles.Add(vehicle);
        });

        await using var reader = database.CreateContext(TimeProvider.System);
        var loaded = await reader.Vehicles.SingleAsync(v => v.Id == vehicle.Id, TestContext.Current.CancellationToken);

        Assert.Equal(2020, loaded.Year.Value);
        Assert.Equal(vehicle.LicensePlate, loaded.LicensePlate);
        Assert.Empty(loaded.DomainEvents);
    }

    [Fact]
    public async Task Part_ComesBackWithMovements_AndAcceptsNewEntries()
    {
        var part = TestData.Part(stock: 4m);
        await SaveAsync(context => context.Parts.Add(part));

        await using (var context = database.CreateContext(TimeProvider.System))
        {
            var loaded = await context.Parts.Include(p => p.StockMovements).SingleAsync(p => p.Id == part.Id, TestContext.Current.CancellationToken);
            Assert.Single(loaded.StockMovements);
            Assert.Equal(4m, loaded.StockQuantity);
            Assert.True(loaded.RegisterEntry(Quantity.Create(1.5m).Value, "Compra", null).IsSuccess);
            Assert.True((await new UnitOfWork(context, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken)).IsSuccess);
        }

        await using var reader = database.CreateContext(TimeProvider.System);
        var reloaded = await reader.Parts.Include(p => p.StockMovements).SingleAsync(p => p.Id == part.Id, TestContext.Current.CancellationToken);
        Assert.Equal(5.5m, reloaded.StockQuantity);
        Assert.Equal(2, reloaded.StockMovements.Count);
        Assert.Equal(part.Code, reloaded.Code);
        Assert.Equal(part.UnitPrice, reloaded.UnitPrice);
    }

    [Fact]
    public async Task ServiceOrderWithItems_ComesBackComplete_AndRaisesEventsAfterLoading()
    {
        var customer = TestData.Customer();
        var vehicle = TestData.Vehicle(customer.Id);
        var service = TestData.Service(120m);
        var part = TestData.Part();
        var order = TestData.ServiceOrder(customer.Id, vehicle.Id, 8001);
        order.StartDiagnosis(null, TestData.Today);
        order.AddServiceItem(service, 2);
        order.AddPartItem(part, Quantity.Create(1.5m).Value);
        order.SendQuote(TestData.Token(), null, TestData.Today);
        order.ApproveQuote(null, TestData.Today);
        await SaveAsync(context =>
        {
            context.Customers.Add(customer);
            context.Vehicles.Add(vehicle);
            context.Services.Add(service);
            context.Parts.Add(part);
            context.ServiceOrders.Add(order);
        });

        await using var context = database.CreateContext(TimeProvider.System);
        var loaded = await context.ServiceOrders
            .Include(o => o.ServiceItems)
            .Include(o => o.PartItems)
            .Include(o => o.Quotes)
            .Include(o => o.StatusHistory)
            .SingleAsync(o => o.Id == order.Id, TestContext.Current.CancellationToken);

        Assert.Equal(order.Number, loaded.Number);
        Assert.Equal(ServiceOrderStatus.InProgress, loaded.Status);
        Assert.Equal(240m, loaded.ServiceItems.Single().Total.Amount);
        Assert.Equal(1.5m, loaded.PartItems.Single().Quantity.Value);
        Assert.Equal(QuoteStatus.Approved, loaded.CurrentQuote!.Status);
        Assert.Null(loaded.CurrentQuote.ApprovalToken);
        Assert.Equal(order.TotalAmount, loaded.TotalAmount);
        Assert.Equal(4, loaded.StatusHistory.Count);
        Assert.Empty(loaded.DomainEvents);

        Assert.True(loaded.Complete(null, TestData.Today.AddHours(2)).IsSuccess);
        Assert.IsType<ServiceOrderCompleted>(Assert.Single(loaded.DomainEvents));
        Assert.True((await new UnitOfWork(context, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken)).IsSuccess);
    }

    [Fact]
    public async Task ServiceOrderWithoutIncludes_StillHasEmptyCollections()
    {
        var customer = TestData.Customer();
        var vehicle = TestData.Vehicle(customer.Id);
        var order = TestData.ServiceOrder(customer.Id, vehicle.Id, 8002);
        await SaveAsync(context =>
        {
            context.Customers.Add(customer);
            context.Vehicles.Add(vehicle);
            context.ServiceOrders.Add(order);
        });

        await using var reader = database.CreateContext(TimeProvider.System);
        var loaded = await reader.ServiceOrders.SingleAsync(o => o.Id == order.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(loaded.ServiceItems);
        Assert.NotNull(loaded.PartItems);
        Assert.NotNull(loaded.Quotes);
        Assert.NotNull(loaded.DomainEvents);
        Assert.Null(loaded.CurrentQuote);
    }

    [Fact]
    public async Task UserAndService_ComeBackWithTheirValues()
    {
        var user = TestData.User();
        var service = TestData.Service(89.9m);
        await SaveAsync(context =>
        {
            context.Users.Add(user);
            context.Services.Add(service);
        });

        await using var reader = database.CreateContext(TimeProvider.System);
        var loadedUser = await reader.Users.SingleAsync(u => u.Id == user.Id, TestContext.Current.CancellationToken);
        var loadedService = await reader.Services.SingleAsync(s => s.Id == service.Id, TestContext.Current.CancellationToken);

        Assert.Equal(user.Email, loadedUser.Email);
        Assert.Equal(user.Role, loadedUser.Role);
        Assert.True(loadedUser.CanLogin);
        Assert.Equal(89.9m, loadedService.BasePrice.Amount);
        Assert.Equal("Inclui filtro", loadedService.Description);
    }

    private async Task SaveAsync(Action<OficinaDbContext> change)
    {
        await using var context = database.CreateContext(TimeProvider.System);
        change(context);
        Assert.True((await new UnitOfWork(context, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken)).IsSuccess);
    }
}
