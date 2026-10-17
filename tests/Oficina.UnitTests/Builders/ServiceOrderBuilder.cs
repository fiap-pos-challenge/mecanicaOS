using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Enums;
using Oficina.Domain.Inventory.ValueObjects;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.ValueObjects;

namespace Oficina.UnitTests.Builders;

public sealed class ServiceOrderBuilder
{
    public static readonly DateTimeOffset OpenedAt = new(2026, 9, 30, 8, 0, 0, TimeSpan.Zero);
    public static readonly ApprovalToken FirstToken = ApprovalToken.Create(new string('a', 64)).Value;
    public static readonly ApprovalToken SecondToken = ApprovalToken.Create(new string('b', 64)).Value;

    private ServiceOrderStatus _status;

    public ServiceOrderBuilder() => _status = ServiceOrderStatus.Received;

    public static Service CreateService(decimal price = 100m, bool active = true)
    {
        var service = Service.Create("Troca de óleo", null, Money.Create(price).Value, 30, 10m, 90).Value;
        if (!active)
        {
            service.Deactivate();
        }

        return service;
    }

    public static Part CreatePart(decimal price = 25m, decimal stock = 10m, bool active = true)
    {
        var part = Part.Create(PartCode.Create("FLT-001").Value, "Filtro de óleo", PartType.Part, "un", Money.Create(price).Value, Money.Zero, 0m, null, null).Value;
        if (stock > 0)
        {
            part.RegisterEntry(Quantity.Create(stock).Value, "Carga inicial", null);
        }

        if (!active)
        {
            part.Deactivate();
        }

        return part;
    }

    public ServiceOrderBuilder InStatus(ServiceOrderStatus status)
    {
        _status = status;
        return this;
    }

    public ServiceOrder Build()
    {
        var order = ServiceOrder.Open(
            ServiceOrderNumber.Create(1001).Value,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Barulho ao frear",
            null,
            OpenedAt).Value;

        if (_status == ServiceOrderStatus.Received)
        {
            return Clean(order);
        }

        order.StartDiagnosis(null, OpenedAt.AddMinutes(10));
        if (_status == ServiceOrderStatus.InDiagnosis)
        {
            return Clean(order);
        }

        order.AddServiceItem(CreateService(), 1);
        order.AddPartItem(CreatePart(), Quantity.Create(2m).Value);
        order.SendQuote(FirstToken, null, OpenedAt.AddMinutes(30));
        if (_status == ServiceOrderStatus.WaitingApproval)
        {
            return Clean(order);
        }

        order.ApproveQuote(null, OpenedAt.AddHours(1));
        if (_status == ServiceOrderStatus.InProgress)
        {
            return Clean(order);
        }

        order.Complete(null, OpenedAt.AddHours(3));
        if (_status == ServiceOrderStatus.Completed)
        {
            return Clean(order);
        }

        order.Deliver(null, OpenedAt.AddHours(4));
        return Clean(order);
    }

    private static ServiceOrder Clean(ServiceOrder order)
    {
        order.ClearDomainEvents();
        return order;
    }
}
