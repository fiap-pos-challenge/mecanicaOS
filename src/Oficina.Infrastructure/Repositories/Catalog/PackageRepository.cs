using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Catalog;

public sealed class PackageRepository(OficinaDbContext context) : IPackageRepository
{
    public Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Packages.SingleOrDefaultAsync(package => package.Id == id, cancellationToken);

    public async Task<IReadOnlyList<string>> ListActiveNamesUsingAsync(CatalogItemKind kind, Guid itemId, CancellationToken cancellationToken) =>
        await context.Packages.AsNoTracking()
            .Where(package => package.IsActive && package.Items.Any(item => item.Kind == kind && item.ItemId == itemId))
            .OrderBy(package => package.Name)
            .Select(package => package.Name)
            .ToListAsync(cancellationToken);

    public void Add(Package package) => context.Packages.Add(package);
}
