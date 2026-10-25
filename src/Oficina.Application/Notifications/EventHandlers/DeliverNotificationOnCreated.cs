using System.Net;
using Oficina.Application.Abstractions.Messaging;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Abstractions.Realtime;
using Oficina.Application.Notifications.Dtos;
using Oficina.Domain.Notifications;

namespace Oficina.Application.Notifications.EventHandlers;

public sealed class DeliverNotificationOnCreated(IUserDirectory directory, NotificationPreferences preferences, IRealtimePublisher realtime, IEmailSender emailSender)
    : IDomainEventHandler<NotificationCreated>
{
    public async Task HandleAsync(NotificationCreated domainEvent, CancellationToken cancellationToken)
    {
        await realtime.PublishAsync(new
        {
            type = "notification",
            notification = new NotificationDto(domainEvent.NotificationId, domainEvent.Type, domainEvent.Title, domainEvent.Body, domainEvent.OccurredAt, domainEvent.Link, domainEvent.AiSuggested, false),
            occurredAt = domainEvent.OccurredAt,
        }, cancellationToken);
        var recipients = await directory.ActiveEmailsAsync(cancellationToken);
        var choices = await preferences.OfManyAsync(recipients.Select(recipient => recipient.UserId).ToList(), cancellationToken);
        foreach (var (userId, email) in recipients.Where(recipient => choices[recipient.UserId].Any(item => item.Type == domainEvent.Type && item.Email)))
        {
            await emailSender.SendAsync(new EmailMessage(email, domainEvent.Title, $"<p>{WebUtility.HtmlEncode(domainEvent.Body)}</p>"), cancellationToken);
        }
    }
}
