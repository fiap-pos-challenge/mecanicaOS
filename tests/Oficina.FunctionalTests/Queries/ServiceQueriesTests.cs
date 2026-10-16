using Oficina.Application.Common.Pagination;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Common.ValueObjects;
using Oficina.FunctionalTests.Fixtures;
using Oficina.Infrastructure.Messaging.Outbox;
using Oficina.Infrastructure.Persistence;
using Oficina.Infrastructure.Queries.Catalog;

namespace Oficina.FunctionalTests.Queries;

[Collection(nameof(DatabaseCollection))]
public sealed class ServiceQueriesTests(SqlServerFixture database) : IAsyncLifetime
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await database.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task ListAsync_ReturnsThePageOrderedByNameWithTheTotal()
    {
        await SaveAsync(NewService("Revisão"), NewService("Alinhamento"), NewService("Balanceamento"));
        await using var context = database.CreateContext(TimeProvider.System);

        var page = await new ServiceQueries(context).ListAsync(new PageRequest(2, 2), includeInactive: false, CancellationToken);

        Assert.Equal("Revisão", Assert.Single(page.Items).Name);
        Assert.Equal(2, page.Page);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(3, page.TotalItems);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task ListAsync_IncludesInactiveServicesOnlyWhenAsked()
    {
        var inactive = NewService("Inativo");
        inactive.Deactivate();
        await SaveAsync(inactive, NewService("Ativo"));
        await using var context = database.CreateContext(TimeProvider.System);
        var queries = new ServiceQueries(context);

        var activeOnly = await queries.ListAsync(new PageRequest(1, 20), includeInactive: false, CancellationToken);
        var all = await queries.ListAsync(new PageRequest(1, 20), includeInactive: true, CancellationToken);

        var active = Assert.Single(activeOnly.Items);
        Assert.Equal("Ativo", active.Name);
        Assert.True(active.IsActive);
        Assert.Equal(1, activeOnly.TotalItems);
        Assert.Equal(new[] { ("Ativo", true), ("Inativo", false) }, all.Items.Select(service => (service.Name, service.IsActive)).ToArray());
        Assert.Equal(2, all.TotalItems);
    }

    [Fact]
    public async Task GetByIdAsync_InactiveService_ReturnsItAsInactive()
    {
        var inactive = NewService("Inativo");
        inactive.Deactivate();
        await SaveAsync(inactive);
        await using var context = database.CreateContext(TimeProvider.System);

        var found = await new ServiceQueries(context).GetByIdAsync(inactive.Id, CancellationToken);

        Assert.NotNull(found);
        Assert.False(found.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTheServiceWithPriceAndAudit()
    {
        var service = TestData.Service(89.9m);
        await SaveAsync(service);
        await using var context = database.CreateContext(TimeProvider.System);

        var found = await new ServiceQueries(context).GetByIdAsync(service.Id, CancellationToken);

        Assert.NotNull(found);
        Assert.Equal(89.90m, found.BasePrice);
        Assert.Equal("Inclui filtro", found.Description);
        Assert.NotEqual(default, found.CreatedAt);
        Assert.Null(await new ServiceQueries(context).GetByIdAsync(Guid.CreateVersion7(), CancellationToken));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    private static Service NewService(string name) => Service.Create(name, null, Money.Create(100m).Value, 30).Value;

    private async Task SaveAsync(params Service[] services)
    {
        await using var context = database.CreateContext(TimeProvider.System);
        context.Services.AddRange(services);
        Assert.True((await new UnitOfWork(context, new OutboxWriter()).CommitAsync(CancellationToken)).IsSuccess);
    }
}
