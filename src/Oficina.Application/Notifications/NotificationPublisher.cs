using Oficina.Domain.Notifications;

namespace Oficina.Application.Notifications;

public sealed class NotificationPublisher(INotificationRepository notifications, TimeProvider timeProvider) : ITeamNotificationPublisher
{
    public async Task PublishAsync(string type, string title, string body, string? link, bool aiSuggested, string? dedupeKey, CancellationToken cancellationToken)
    {
        if (dedupeKey is not null && await notifications.DedupeExistsAsync(dedupeKey, cancellationToken))
        {
            return;
        }

        notifications.Add(Notification.Create(type, title, body, link, aiSuggested, dedupeKey, timeProvider.GetUtcNow()));
    }
}
