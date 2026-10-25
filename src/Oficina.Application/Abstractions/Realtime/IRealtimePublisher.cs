namespace Oficina.Application.Abstractions.Realtime;

public interface IRealtimePublisher
{
    Task PublishAsync(object realtimeEvent, CancellationToken cancellationToken);
}
