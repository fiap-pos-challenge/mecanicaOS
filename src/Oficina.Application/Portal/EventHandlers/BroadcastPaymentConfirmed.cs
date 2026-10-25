using Oficina.Application.Abstractions.Messaging;
using Oficina.Application.Abstractions.Realtime;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Portal.Events;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Application.Portal.EventHandlers;

public sealed class BroadcastPaymentConfirmed(IServiceOrderRepository serviceOrders, IRealtimePublisher realtime) : IDomainEventHandler<PaymentConfirmed>
{
    public async Task HandleAsync(PaymentConfirmed domainEvent, CancellationToken cancellationToken)
    {
        var order = await serviceOrders.GetByIdAsync(domainEvent.ServiceOrderId, cancellationToken);
        await realtime.PublishAsync(new { type = "paymentConfirmed", serviceOrderId = domainEvent.ServiceOrderId, number = order?.Number.Value, amount = Money.ToCents(domainEvent.Amount), occurredAt = domainEvent.OccurredAt }, cancellationToken);
    }
}
