using Oficina.Application.Catalog.Queries;
using Oficina.Application.Common.Pagination;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Enums;
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

        var page = await new ServiceQueries(context).ListAsync(new PageRequest(2, 2), new CatalogFilter(null, true), CancellationToken);

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

        var activeOnly = await queries.ListAsync(new PageRequest(1, 20), new CatalogFilter(null, true), CancellationToken);
        var all = await queries.ListAsync(new PageRequest(1, 20), new CatalogFilter(null, null), CancellationToken);

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

    [Fact]
    public async Task ListAsync_FiltersByTermAndStatus()
    {
        var inactive = NewService("Troca de pastilha");
        inactive.Deactivate();
        await SaveAsync(inactive, NewService("Troca de óleo"), NewService("Alinhamento"));
        await using var context = database.CreateContext(TimeProvider.System);
        var queries = new ServiceQueries(context);

        var byTerm = await queries.ListAsync(new PageRequest(1, 20), new CatalogFilter("Troca", null), CancellationToken);
        var inactiveOnly = await queries.ListAsync(new PageRequest(1, 20), new CatalogFilter(null, false), CancellationToken);

        Assert.Equal(new[] { "Troca de óleo", "Troca de pastilha" }, byTerm.Items.Select(service => service.Name).ToArray());
        Assert.Equal("Troca de pastilha", Assert.Single(inactiveOnly.Items).Name);
    }

    [Fact]
    public async Task PriceHistory_ListAsync_ReturnsNewestFirstWithAuthorNameAndHidesCost()
    {
        var service = NewService("Alinhamento");
        var author = TestData.User("autor.preco@oficina.com");
        var partId = Guid.CreateVersion7();
        await SaveAsync(service);
        await using (var setup = database.CreateContext(TimeProvider.System))
        {
            setup.Users.Add(author);
            setup.PriceChanges.Add(PriceChange.Record(CatalogItemKind.Service, service.Id, PriceField.BasePrice, null, Money.Create(100m).Value, null, DateTimeOffset.UnixEpoch)!);
            setup.PriceChanges.Add(PriceChange.Record(CatalogItemKind.Service, service.Id, PriceField.BasePrice, Money.Create(100m).Value, Money.Create(120m).Value, author.Id, DateTimeOffset.UnixEpoch.AddDays(1))!);
            setup.PriceChanges.Add(PriceChange.Record(CatalogItemKind.Part, partId, PriceField.Cost, null, Money.Create(10m).Value, null, DateTimeOffset.UnixEpoch)!);
            setup.PriceChanges.Add(PriceChange.Record(CatalogItemKind.Part, partId, PriceField.UnitPrice, null, Money.Create(20m).Value, null, DateTimeOffset.UnixEpoch)!);
            await setup.SaveChangesAsync(CancellationToken);
        }

        await using var context = database.CreateContext(TimeProvider.System);
        var history = new PriceHistoryQueries(context);
        var changes = await history.ListAsync(CatalogItemKind.Service, service.Id, includeCost: false, CancellationToken);

        Assert.Equal(2, changes.Count);
        Assert.Equal((100m, 120m, author.Name), (changes[0].OldValue!.Value, changes[0].NewValue, changes[0].ChangedBy));
        Assert.Equal(PriceHistoryQueries.UnknownAuthor, changes[1].ChangedBy);
        Assert.Null(changes[1].OldValue);
        Assert.All(changes, change => Assert.Equal(nameof(PriceField.BasePrice), change.Field));
        Assert.Equal("UnitPrice", Assert.Single(await history.ListAsync(CatalogItemKind.Part, partId, includeCost: false, CancellationToken)).Field);
        Assert.Equal(2, (await history.ListAsync(CatalogItemKind.Part, partId, includeCost: true, CancellationToken)).Count);
    }

    private static Service NewService(string name) => Service.Create(name, null, Money.Create(100m).Value, 30, 10m, 90).Value;

    private async Task SaveAsync(params Service[] services)
    {
        await using var context = database.CreateContext(TimeProvider.System);
        context.Services.AddRange(services);
        Assert.True((await new UnitOfWork(context, new OutboxWriter()).CommitAsync(CancellationToken)).IsSuccess);
    }
}
