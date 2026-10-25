using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.Domain.Catalog.Entities;

public sealed class PriceChange
{
    private PriceChange(Guid id, CatalogItemKind kind, Guid itemId, PriceField field, decimal? oldValue, decimal newValue, Guid? changedBy, DateTimeOffset changedAt)
    {
        Id = id;
        Kind = kind;
        ItemId = itemId;
        Field = field;
        OldValue = oldValue;
        NewValue = newValue;
        ChangedBy = changedBy;
        ChangedAt = changedAt;
    }

    public Guid Id { get; private init; }

    public CatalogItemKind Kind { get; private init; }

    public Guid ItemId { get; private init; }

    public PriceField Field { get; private init; }

    public decimal? OldValue { get; private init; }

    public decimal NewValue { get; private init; }

    public Guid? ChangedBy { get; private init; }

    public DateTimeOffset ChangedAt { get; private init; }

    public static PriceChange? Record(CatalogItemKind kind, Guid itemId, PriceField field, Money? oldValue, Money newValue, Guid? changedBy, DateTimeOffset now) =>
        oldValue == newValue
            ? null
            : new PriceChange(Guid.CreateVersion7(), kind, itemId, field, oldValue?.Amount, newValue.Amount, changedBy, now);
}
