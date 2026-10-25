using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Oficina.Application.Common.Pagination;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.FunctionalTests.Fixtures;
using Oficina.Infrastructure.Messaging.Outbox;
using Oficina.Infrastructure.Persistence;
using Oficina.Infrastructure.Queries.Customers;

namespace Oficina.FunctionalTests.Queries;

[Collection(nameof(DatabaseCollection))]
public sealed class CustomerQueriesTests(SqlServerFixture database) : IAsyncLifetime
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await database.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task ListAsync_ReturnsThePageOrderedByNameWithTheTotal()
    {
        await SaveAsync(NewCustomer("Carla", "52998224725"), NewCustomer("Ana", "11222333000181"), NewCustomer("Bruno", "39053344705"));
        await using var context = database.CreateContext(TimeProvider.System);

        var page = await new CustomerQueries(context).ListAsync(new PageRequest(2, 2), CancellationToken);

        Assert.Equal("Carla", Assert.Single(page.Items).Name);
        Assert.Equal(2, page.Page);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(3, page.TotalItems);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task ListAsync_PagesInTheDatabaseBeforeMaterializing()
    {
        await SaveAsync(NewCustomer("Carla", "52998224725"), NewCustomer("Ana", "11222333000181"));
        var capture = new CommandCapture();
        var options = new DbContextOptionsBuilder<OficinaDbContext>().UseSqlServer(database.ConnectionString).AddInterceptors(capture).Options;
        await using var context = new OficinaDbContext(options);

        await new CustomerQueries(context).ListAsync(new PageRequest(2, 1), CancellationToken);

        var pageQuery = Assert.Single(capture.Commands, command => command.Contains("[customers]", StringComparison.Ordinal) && !command.Contains("COUNT(", StringComparison.Ordinal));
        Assert.Contains("OFFSET", pageQuery, StringComparison.Ordinal);
        Assert.Contains("FETCH NEXT", pageQuery, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListAsync_IgnoresDeletedCustomers()
    {
        var deleted = NewCustomer("Removido", "52998224725");
        await SaveAsync(deleted, NewCustomer("Ativo", "11222333000181"));
        await using (var context = database.CreateContext(TimeProvider.System))
        {
            context.Customers.Remove(await context.Customers.FindAsync(new object[] { deleted.Id }, CancellationToken) ?? throw new InvalidOperationException());
            await CommitAsync(context);
        }

        await using var reader = database.CreateContext(TimeProvider.System);
        var page = await new CustomerQueries(reader).ListAsync(new PageRequest(1, 20), CancellationToken);

        Assert.Equal("Ativo", Assert.Single(page.Items).Name);
        Assert.Null(await new CustomerQueries(reader).GetByIdAsync(deleted.Id, CancellationToken));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTheCustomerWithAddress()
    {
        var customer = TestData.Customer(address: TestData.FullAddress());
        await SaveAsync(customer);
        await using var context = database.CreateContext(TimeProvider.System);

        var found = await new CustomerQueries(context).GetByIdAsync(customer.Id, CancellationToken);

        Assert.NotNull(found);
        Assert.Equal("•••.982.247-••", found.Document);
        Assert.Equal("SP", found.Address!.State);
        Assert.NotEqual(default, found.CreatedAt);
        Assert.Null(await new CustomerQueries(context).GetByIdAsync(Guid.CreateVersion7(), CancellationToken));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByDocumentAsync_FindsOnlyTheMatchingDocument()
    {
        var customer = TestData.Customer();
        await SaveAsync(customer);
        await using var context = database.CreateContext(TimeProvider.System);
        var queries = new CustomerQueries(context);

        Assert.Equal(customer.Id, (await queries.GetByDocumentAsync(Document.Create("529.982.247-25").Value, CancellationToken))?.Id);
        Assert.Null(await queries.GetByDocumentAsync(Document.Create("11222333000181").Value, CancellationToken));
    }

    private static Customer NewCustomer(string name, string document) =>
        Customer.Create(name, Document.Create(document).Value, null, Phone.Create("11987654321").Value, null).Value;

    private static async Task CommitAsync(OficinaDbContext context) =>
        Assert.True((await new UnitOfWork(context, new OutboxWriter()).CommitAsync(CancellationToken)).IsSuccess);

    private async Task SaveAsync(params Customer[] customers)
    {
        await using var context = database.CreateContext(TimeProvider.System);
        context.Customers.AddRange(customers);
        await CommitAsync(context);
    }

    private sealed class CommandCapture : DbCommandInterceptor
    {
        public List<string> Commands { get; } = new();

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
