namespace Oficina.Application.Notifications;

public interface ITeamNotificationPublisher
{
    Task PublishAsync(string type, string title, string body, string? link, bool aiSuggested, string? dedupeKey, CancellationToken cancellationToken);
}
