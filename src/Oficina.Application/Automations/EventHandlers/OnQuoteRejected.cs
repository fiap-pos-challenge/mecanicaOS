using Oficina.Application.Abstractions.Messaging;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Notifications;
using Oficina.Domain.ServiceOrders.Events;

namespace Oficina.Application.Automations.EventHandlers;

public sealed class OnQuoteRejected(ITeamNotificationPublisher notifications, IUnitOfWork unitOfWork) : IDomainEventHandler<QuoteRejected>
{
    public async Task HandleAsync(QuoteRejected domainEvent, CancellationToken cancellationToken)
    {
        await notifications.PublishAsync(NotificationTypes.QuoteAnswered, AutomationEventTexts.QuoteRejected(domainEvent.Number), AutomationEventTexts.QuoteRejectedBody, "/quotes", false, null, cancellationToken);
        await unitOfWork.CommitOrThrowAsync(cancellationToken);
    }
}
