using Oficina.Application.Abstractions.Messaging;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Realtime;
using Oficina.Application.Notifications;
using Oficina.Domain.ServiceOrders.Events;

namespace Oficina.Application.Automations;

public sealed class OnServiceOrderStatusChanged(AutomationService automations, INotificationPublisher notifications, IRealtimePublisher realtime, IUnitOfWork unitOfWork)
    : IDomainEventHandler<ServiceOrderStatusChanged>
{
    public async Task HandleAsync(ServiceOrderStatusChanged domainEvent, CancellationToken cancellationToken)
    {
        await realtime.PublishAsync(new { type = "serviceOrderStatusChanged", serviceOrderId = domainEvent.ServiceOrderId, number = domainEvent.Number, status = domainEvent.Status, occurredAt = domainEvent.OccurredAt }, cancellationToken);
        await automations.FireAsync("ServiceOrderStatusChanged", domainEvent.ServiceOrderId, $"status:{domainEvent.ServiceOrderId:N}:{domainEvent.Status}:{domainEvent.OccurredAt.ToUnixTimeSeconds()}", cancellationToken);
        if (domainEvent.Status == "Delivered")
        {
            await automations.FireAsync("ServiceOrderDelivered", domainEvent.ServiceOrderId, $"delivered:{domainEvent.ServiceOrderId:N}", cancellationToken);
        }

        if (domainEvent.Status is "Completed" or "WaitingApproval")
        {
            var title = domainEvent.Status == "Completed" ? $"OS nº {domainEvent.Number} pronta para entrega" : $"OS nº {domainEvent.Number} aguarda aprovação do cliente";
            await notifications.PublishAsync("ServiceOrderChanged", title, "Abra a OS para ver os detalhes.", "/service-orders", false, null, cancellationToken);
        }

        await unitOfWork.CommitAsync(cancellationToken);
    }
}

public sealed class OnQuoteSent(AutomationService automations, IUnitOfWork unitOfWork) : IDomainEventHandler<QuoteSent>
{
    public async Task HandleAsync(QuoteSent domainEvent, CancellationToken cancellationToken)
    {
        await automations.FireAsync("QuoteSent", domainEvent.ServiceOrderId, $"quote:{domainEvent.ServiceOrderId:N}:{domainEvent.QuoteVersion}:{domainEvent.OccurredAt.ToUnixTimeSeconds()}", cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}

public sealed class OnQuoteApproved(INotificationPublisher notifications, IUnitOfWork unitOfWork) : IDomainEventHandler<QuoteApproved>
{
    public async Task HandleAsync(QuoteApproved domainEvent, CancellationToken cancellationToken)
    {
        await notifications.PublishAsync("QuoteAnswered", $"Orçamento da OS nº {domainEvent.Number} aprovado", "O cliente aprovou o orçamento. A OS pode seguir para execução.", "/service-orders", false, null, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}

public sealed class OnQuoteRejected(INotificationPublisher notifications, IUnitOfWork unitOfWork) : IDomainEventHandler<QuoteRejected>
{
    public async Task HandleAsync(QuoteRejected domainEvent, CancellationToken cancellationToken)
    {
        await notifications.PublishAsync("QuoteAnswered", $"Orçamento da OS nº {domainEvent.Number} recusado", "O cliente recusou o orçamento. Revise o diagnóstico ou crie uma nova versão.", "/quotes", false, null, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
