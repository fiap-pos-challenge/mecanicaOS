using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;
using Oficina.Infrastructure.Messaging.Outbox;
using Oficina.Infrastructure.Persistence;
using Oficina.Infrastructure.Repositories.Catalog;
using Oficina.Infrastructure.Repositories.Customers;
using Oficina.Infrastructure.Repositories.Identity;
using Oficina.Infrastructure.Repositories.Inventory;
using Oficina.Infrastructure.Repositories.ServiceOrders;

namespace Oficina.FunctionalTests.Persistence;

[Collection(nameof(DatabaseCollection))]
public sealed class RepositoryTests(SqlServerFixture database) : IAsyncLifetime
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await database.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CustomerRepository_AddAndGetById_RoundTrips()
    {
        var customer = TestData.Customer(address: TestData.FullAddress());
        await using (var context = database.CreateContext(TimeProvider.System))
        {
            new CustomerRepository(context).Add(customer);
            await CommitAsync(context);
        }

        await using var reader = database.CreateContext(TimeProvider.System);
        var loaded = await new CustomerRepository(reader).GetByIdAsync(customer.Id, CancellationToken);

        Assert.NotNull(loaded);
        Assert.Equal(customer.Name, loaded.Name);
        Assert.Null(await new CustomerRepository(reader).GetByIdAsync(Guid.CreateVersion7(), CancellationToken));
    }

    [Fact]
    public async Task CustomerRepository_GetByDocument_FindsOnlyNotDeleted()
    {
        var customer = TestData.Customer();
        await SaveAsync(context => context.Customers.Add(customer));

        await using (var reader = database.CreateContext(TimeProvider.System))
        {
            var found = await new CustomerRepository(reader).GetByDocumentAsync(Document.Create("529.982.247-25").Value, CancellationToken);
            Assert.Equal(customer.Id, found?.Id);
            Assert.Null(await new CustomerRepository(reader).GetByDocumentAsync(Document.Create("11144477735").Value, CancellationToken));
        }

        await using (var context = database.CreateContext(TimeProvider.System))
        {
            var repository = new CustomerRepository(context);
            var loaded = await repository.GetByIdAsync(customer.Id, CancellationToken);
            Assert.NotNull(loaded);
            repository.Remove(loaded);
            await CommitAsync(context);
        }

        await using var afterDelete = database.CreateContext(TimeProvider.System);
        Assert.Null(await new CustomerRepository(afterDelete).GetByDocumentAsync(customer.Document, CancellationToken));
        Assert.Equal(1, await database.CountRowsAsync("customers", customer.Id));
    }

    [Fact]
    public async Task VehicleRepository_AddAndGetById_RoundTrips()
    {
        var customer = TestData.Customer();
        var vehicle = TestData.Vehicle(customer.Id);
        await using (var context = database.CreateContext(TimeProvider.System))
        {
            context.Customers.Add(customer);
            new VehicleRepository(context).Add(vehicle);
            await CommitAsync(context);
        }

        await using var reader = database.CreateContext(TimeProvider.System);
        var loaded = await new VehicleRepository(reader).GetByIdAsync(vehicle.Id, CancellationToken);

        Assert.Equal(vehicle.LicensePlate, loaded?.LicensePlate);
    }

    [Fact]
    public async Task ServiceRepository_AddAndGetById_RoundTrips()
    {
        var service = TestData.Service();
        await using (var context = database.CreateContext(TimeProvider.System))
        {
            new ServiceRepository(context).Add(service);
            await CommitAsync(context);
        }

        await using var reader = database.CreateContext(TimeProvider.System);
        var loaded = await new ServiceRepository(reader).GetByIdAsync(service.Id, CancellationToken);

        Assert.Equal(service.BasePrice, loaded?.BasePrice);
    }

    [Fact]
    public async Task PartRepository_AddAndGetById_LoadsTheMovements()
    {
        var part = TestData.Part(stock: 3m);
        await using (var context = database.CreateContext(TimeProvider.System))
        {
            new PartRepository(context).Add(part);
            await CommitAsync(context);
        }

        await using var reader = database.CreateContext(TimeProvider.System);
        var loaded = await new PartRepository(reader).GetByIdAsync(part.Id, CancellationToken);

        Assert.NotNull(loaded);
        Assert.Single(loaded.StockMovements);
        Assert.Equal(3m, loaded.StockQuantity);
    }

    [Fact]
    public async Task UserRepository_AddGetByIdAndGetByEmail_RoundTrip()
    {
        var user = TestData.User("mecanico@oficina.com", Role.Mechanic);
        await using (var context = database.CreateContext(TimeProvider.System))
        {
            new UserRepository(context).Add(user);
            await CommitAsync(context);
        }

        await using var reader = database.CreateContext(TimeProvider.System);
        var repository = new UserRepository(reader);
        Assert.Equal(user.Id, (await repository.GetByIdAsync(user.Id, CancellationToken))?.Id);
        Assert.Equal(user.Id, (await repository.GetByEmailAsync(Email.Create("MECANICO@oficina.com").Value, CancellationToken))?.Id);
        Assert.Null(await repository.GetByEmailAsync(Email.Create("outro@oficina.com").Value, CancellationToken));
    }

    [Fact]
    public async Task UserRepository_CountActiveAdmins_IgnoresInactiveDeletedAndOtherRoles()
    {
        var activeAdmin = TestData.User("admin1@oficina.com");
        var inactiveAdmin = TestData.User("admin2@oficina.com");
        inactiveAdmin.Deactivate();
        var deletedAdmin = TestData.User("admin3@oficina.com");
        var attendant = TestData.User("atendente@oficina.com", Role.Attendant);
        await SaveAsync(context => context.Users.AddRange(activeAdmin, inactiveAdmin, deletedAdmin, attendant));
        await using (var context = database.CreateContext(TimeProvider.System))
        {
            var repository = new UserRepository(context);
            var loaded = await repository.GetByIdAsync(deletedAdmin.Id, CancellationToken);
            Assert.NotNull(loaded);
            repository.Remove(loaded);
            await CommitAsync(context);
        }

        await using var reader = database.CreateContext(TimeProvider.System);

        Assert.Equal(1, await new UserRepository(reader).CountActiveAdminsAsync(CancellationToken));
    }

    [Fact]
    public async Task ServiceOrderRepository_AddAndGetById_LoadsTheWholeAggregate()
    {
        var customer = TestData.Customer();
        var vehicle = TestData.Vehicle(customer.Id);
        var service = TestData.Service();
        var part = TestData.Part();
        var order = TestData.ServiceOrder(customer.Id, vehicle.Id, 5001);
        order.StartDiagnosis(null, TestData.Today);
        order.AddServiceItem(service, 1);
        order.AddPartItem(part, Quantity.Create(1m).Value);
        order.SendQuote(TestData.Token(), null, TestData.Today);
        await using (var context = database.CreateContext(TimeProvider.System))
        {
            context.Customers.Add(customer);
            context.Vehicles.Add(vehicle);
            context.Services.Add(service);
            context.Parts.Add(part);
            new ServiceOrderRepository(context).Add(order);
            await CommitAsync(context);
        }

        await using var reader = database.CreateContext(TimeProvider.System);
        var loaded = await new ServiceOrderRepository(reader).GetByIdAsync(order.Id, CancellationToken);

        Assert.NotNull(loaded);
        Assert.Single(loaded.ServiceItems);
        Assert.Single(loaded.PartItems);
        Assert.Single(loaded.Quotes);
        Assert.Equal(3, loaded.StatusHistory.Count);
        Assert.True(loaded.MatchesApprovalToken(TestData.Token()));
    }

    [Fact]
    public async Task ServiceOrderRepository_NextNumber_ComesFromTheSequenceAndIncreases()
    {
        await using var context = database.CreateContext(TimeProvider.System);
        var repository = new ServiceOrderRepository(context);

        var first = await repository.NextNumberAsync(CancellationToken);
        var second = await repository.NextNumberAsync(CancellationToken);

        Assert.True(first.Value >= 1);
        Assert.Equal(first.Value + 1, second.Value);
    }

    [Fact]
    public async Task ServiceOrderRepository_HasOpenOrder_ForVehicleAndCustomer()
    {
        var customer = TestData.Customer();
        var vehicle = TestData.Vehicle(customer.Id);
        var otherCustomer = TestData.Customer("11144477735");
        var otherVehicle = TestData.Vehicle(otherCustomer.Id, "ABC1234");
        await SaveAsync(context =>
        {
            context.Customers.AddRange(customer, otherCustomer);
            context.Vehicles.AddRange(vehicle, otherVehicle);
            context.ServiceOrders.Add(TestData.ServiceOrder(customer.Id, vehicle.Id, 5101));
        });

        await using var reader = database.CreateContext(TimeProvider.System);
        var repository = new ServiceOrderRepository(reader);

        Assert.True(await repository.HasOpenOrderForVehicleAsync(vehicle.Id, CancellationToken));
        Assert.True(await repository.HasOpenOrderForCustomerAsync(customer.Id, CancellationToken));
        Assert.False(await repository.HasOpenOrderForVehicleAsync(otherVehicle.Id, CancellationToken));
        Assert.False(await repository.HasOpenOrderForCustomerAsync(otherCustomer.Id, CancellationToken));
    }

    [Fact]
    public async Task ServiceOrderRepository_HasOpenOrder_IgnoresDeliveredOrders()
    {
        var customer = TestData.Customer();
        var vehicle = TestData.Vehicle(customer.Id);
        var service = TestData.Service();
        var order = TestData.ServiceOrder(customer.Id, vehicle.Id, 5201);
        order.StartDiagnosis(null, TestData.Today);
        order.AddServiceItem(service, 1);
        order.SendQuote(TestData.Token(), null, TestData.Today);
        order.ApproveQuote(null, TestData.Today);
        order.Complete(null, TestData.Today);
        order.Deliver(null, TestData.Today);
        await SaveAsync(context =>
        {
            context.Customers.Add(customer);
            context.Vehicles.Add(vehicle);
            context.Services.Add(service);
            context.ServiceOrders.Add(order);
        });

        await using var reader = database.CreateContext(TimeProvider.System);
        var repository = new ServiceOrderRepository(reader);

        Assert.False(await repository.HasOpenOrderForVehicleAsync(vehicle.Id, CancellationToken));
        Assert.False(await repository.HasOpenOrderForCustomerAsync(customer.Id, CancellationToken));
    }

    private static async Task CommitAsync(OficinaDbContext context) =>
        Assert.True((await new UnitOfWork(context, new OutboxWriter()).CommitAsync(CancellationToken)).IsSuccess);

    private async Task SaveAsync(Action<OficinaDbContext> change)
    {
        await using var context = database.CreateContext(TimeProvider.System);
        change(context);
        await CommitAsync(context);
    }
}
