using Microsoft.EntityFrameworkCore;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Queries;
using Oficina.Application.Common.Pagination;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.Catalog;

public sealed class PackageQueries(OficinaDbContext context) : IPackageQueries
{
    public async Task<PackageDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var package = await context.Packages.AsNoTracking().SingleOrDefaultAsync(package => package.Id == id, cancellationToken);
        return package is null ? null : (await ToDtosAsync([package], cancellationToken))[0];
    }

    public async Task<PagedResult<PackageDto>> ListAsync(PageRequest page, CatalogFilter filter, CancellationToken cancellationToken)
    {
        var packages = context.Packages.AsNoTracking();
        if (filter.IsActive is { } isActive)
        {
            packages = packages.Where(package => package.IsActive == isActive);
        }

        if (filter.Term is { } term)
        {
            packages = packages.Where(package => package.Name.Contains(term));
        }

        var totalItems = await packages.CountAsync(cancellationToken);
        var items = await packages
            .OrderBy(package => package.Name)
            .ThenBy(package => package.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PackageDto>(await ToDtosAsync(items, cancellationToken), page.Page, page.PageSize, totalItems);
    }

    private async Task<IReadOnlyList<PackageDto>> ToDtosAsync(IReadOnlyList<Package> packages, CancellationToken cancellationToken)
    {
        var lines = packages.SelectMany(package => package.Items).ToList();
        var serviceIds = lines.Where(line => line.Kind == CatalogItemKind.Service).Select(line => line.ItemId).Distinct().ToList();
        var partIds = lines.Where(line => line.Kind == CatalogItemKind.Part).Select(line => line.ItemId).Distinct().ToList();
        var services = await context.Services.AsNoTracking().IgnoreQueryFilters()
            .Where(service => serviceIds.Contains(service.Id))
            .ToDictionaryAsync(service => service.Id, service => (service.Name, service.BasePrice), cancellationToken);
        var parts = await context.Parts.AsNoTracking().IgnoreQueryFilters()
            .Where(part => partIds.Contains(part.Id))
            .ToDictionaryAsync(part => part.Id, part => (part.Name, Price: part.UnitPrice), cancellationToken);

        return packages.Select(package =>
        {
            var items = package.Items.Select(line =>
            {
                var (name, price) = line.Kind == CatalogItemKind.Service
                    ? services.GetValueOrDefault(line.ItemId, (string.Empty, Money.Zero))
                    : parts.GetValueOrDefault(line.ItemId, (string.Empty, Money.Zero));
                return (Dto: new PackageItemDto(line.Kind, line.ItemId, line.Quantity.Value, name, price.Amount), Total: price.Multiply(line.Quantity));
            }).ToList();
            var itemsTotal = items.Aggregate(Money.Zero, (sum, item) => sum.Add(item.Total)).Amount;
            return new PackageDto(
                package.Id,
                package.Name,
                package.Description,
                items.Select(item => item.Dto).ToList(),
                package.Price.Amount,
                itemsTotal,
                itemsTotal - package.Price.Amount,
                package.MinMileage is null && package.MaxMileage is null ? null : new PackageSuggestionDto(package.MinMileage, package.MaxMileage),
                package.IsActive,
                package.CreatedAt,
                package.UpdatedAt);
        }).ToList();
    }
}
