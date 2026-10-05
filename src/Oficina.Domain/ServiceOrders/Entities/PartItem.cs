using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.Domain.ServiceOrders.Entities;

public sealed class PartItem : Entity
{
    private PartItem(Guid id, Guid serviceOrderId, Guid partId, string description, Money unitPrice, Quantity quantity)
        : base(id)
    {
        ServiceOrderId = serviceOrderId;
        PartId = partId;
        Description = description;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid ServiceOrderId { get; private set; }

    public Guid PartId { get; private set; }

    public string Description { get; private set; }

    public Money UnitPrice { get; private set; }

    public Quantity Quantity { get; private set; }

    public Money Total => UnitPrice.Multiply(Quantity);

    internal static PartItem Create(Guid serviceOrderId, Guid partId, string description, Money unitPrice, Quantity quantity) =>
        new(Guid.CreateVersion7(), serviceOrderId, partId, description, unitPrice, quantity);
}
