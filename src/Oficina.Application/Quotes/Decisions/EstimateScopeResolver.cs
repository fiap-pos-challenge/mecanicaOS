using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.ServiceOrders.ValueObjects;

namespace Oficina.Application.Quotes.Decisions;

public sealed class EstimateScopeResolver(IServiceRepository services, IPartRepository parts, IPackageRepository packages)
{
    public async Task<QuoteScope> ResolveAsync(IReadOnlyCollection<EstimateLine> lines, decimal total, CancellationToken cancellationToken)
    {
        var serviceLines = new List<(Guid Id, int Quantity)>();
        var partLines = new List<(Guid Id, decimal Quantity)>();
        foreach (var line in lines)
        {
            if (line.Kind == EstimateLineKind.Package && await packages.GetByIdAsync(line.ItemId, cancellationToken) is { } package)
            {
                serviceLines.AddRange(package.Items.Where(item => item.Kind == CatalogItemKind.Service).Select(item => (item.ItemId, (int)decimal.Ceiling(item.Quantity.Value * line.Quantity))));
                partLines.AddRange(package.Items.Where(item => item.Kind == CatalogItemKind.Part).Select(item => (item.ItemId, item.Quantity.Value * line.Quantity)));
            }
            else if (line.Kind == EstimateLineKind.Service)
            {
                serviceLines.Add((line.ItemId, (int)decimal.Ceiling(line.Quantity)));
            }
            else if (line.Kind == EstimateLineKind.Part)
            {
                partLines.Add((line.ItemId, line.Quantity));
            }
        }

        var serviceEntities = await services.ListByIdsAsync(serviceLines.Select(line => line.Id).Distinct().ToList(), cancellationToken);
        var partEntities = await parts.ListByIdsAsync(partLines.Select(line => line.Id).Distinct().ToList(), cancellationToken);
        var serviceItems = serviceLines.GroupBy(line => line.Id)
            .Select(group => (Service: serviceEntities.FirstOrDefault(service => service.Id == group.Key), Quantity: group.Sum(line => line.Quantity)))
            .Where(item => item.Service is not null)
            .Select(item => (item.Service!, item.Quantity))
            .ToList();
        var partItems = partLines.GroupBy(line => line.Id)
            .Select(group => (Part: partEntities.FirstOrDefault(part => part.Id == group.Key), Quantity: group.Sum(line => line.Quantity)))
            .Where(item => item.Part is not null)
            .Select(item => (item.Part!, Quantity.Create(item.Quantity).Value))
            .ToList();
        return new QuoteScope(serviceItems, partItems, Money.Create(total).Value);
    }
}
