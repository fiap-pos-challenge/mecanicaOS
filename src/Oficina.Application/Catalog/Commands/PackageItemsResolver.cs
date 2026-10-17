using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Catalog.Commands;

public sealed class PackageItemsResolver(IServiceRepository services, IPartRepository parts)
{
    public async Task<Result<IReadOnlyCollection<PackageItemInput>>> ResolveAsync(IReadOnlyList<PackageItemRequest> inputs, CancellationToken cancellationToken)
    {
        var items = new List<PackageItemInput>();
        foreach (var input in inputs)
        {
            var quantity = Quantity.Create(input.Quantity);
            if (quantity.Error is not null || input.Kind == CatalogItemKind.Package)
            {
                return PackageErrors.InvalidItems;
            }

            items.Add(new PackageItemInput(input.Kind, input.ItemId, quantity.Value));
        }

        var serviceIds = IdsOf(items, CatalogItemKind.Service);
        var partIds = IdsOf(items, CatalogItemKind.Part);
        var activeServices = (await services.ListByIdsAsync(serviceIds, cancellationToken)).Count(service => service.IsActive);
        var activeParts = (await parts.ListByIdsAsync(partIds, cancellationToken)).Count(part => part.IsActive);
        if (activeServices != serviceIds.Count || activeParts != partIds.Count)
        {
            return PackageErrors.InvalidItems;
        }

        return items;
    }

    private static List<Guid> IdsOf(IEnumerable<PackageItemInput> items, CatalogItemKind kind) =>
        items.Where(item => item.Kind == kind).Select(item => item.ItemId).Distinct().ToList();
}
