using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.Domain.ServiceOrders.Entities;

public sealed class ServiceItem : Entity
{
    private ServiceItem(Guid id, Guid serviceOrderId, Guid serviceId, string description, Money unitPrice, int quantity)
        : base(id)
    {
        ServiceOrderId = serviceOrderId;
        ServiceId = serviceId;
        Description = description;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid ServiceOrderId { get; private set; }

    public Guid ServiceId { get; private set; }

    public string Description { get; private set; }

    public Money UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public Money Total => UnitPrice.Multiply(Quantity);

    internal static ServiceItem Create(Guid serviceOrderId, Guid serviceId, string description, Money unitPrice, int quantity) =>
        new(Guid.CreateVersion7(), serviceOrderId, serviceId, description, unitPrice, quantity);
}
