using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.Domain.Catalog.Entities;

public sealed class PackageItem
{
    private PackageItem(Guid id, CatalogItemKind kind, Guid itemId, Quantity quantity)
    {
        Id = id;
        Kind = kind;
        ItemId = itemId;
        Quantity = quantity;
    }

    public Guid Id { get; private init; }

    public CatalogItemKind Kind { get; private init; }

    public Guid ItemId { get; private init; }

    public Quantity Quantity { get; private init; }

    internal static PackageItem Create(CatalogItemKind kind, Guid itemId, Quantity quantity) => new(Guid.CreateVersion7(), kind, itemId, quantity);
}
