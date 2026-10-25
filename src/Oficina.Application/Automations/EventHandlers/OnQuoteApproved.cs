using Oficina.Application.Abstractions.Messaging;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Notifications;
using Oficina.Domain.ServiceOrders.Events;

namespace Oficina.Application.Automations.EventHandlers;

public sealed class OnQuoteApproved(ITeamNotificationPublisher notifications, IUnitOfWork unitOfWork) : IDomainEventHandler<QuoteApproved>
{
    public async Task HandleAsync(QuoteApproved domainEvent, CancellationToken cancellationToken)
    {
        await notifications.PublishAsync(NotificationTypes.QuoteAnswered, AutomationEventTexts.QuoteApproved(domainEvent.Number), AutomationEventTexts.QuoteApprovedBody, "/service-orders", false, null, cancellationToken);
        await unitOfWork.CommitOrThrowAsync(cancellationToken);
    }
}
