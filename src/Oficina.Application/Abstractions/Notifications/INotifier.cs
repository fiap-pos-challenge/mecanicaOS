namespace Oficina.Application.Abstractions.Notifications;

public interface INotifier
{
    Task NotifyServiceOrderCompletedAsync(long serviceOrderNumber, CancellationToken cancellationToken);
}
