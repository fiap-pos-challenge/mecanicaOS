using Microsoft.AspNetCore.SignalR;
using Oficina.Application.Abstractions.Realtime;

namespace Oficina.Api.Realtime;

public sealed class SignalRRealtimePublisher(IHubContext<RealtimeHub> hub) : IRealtimePublisher
{
    public Task PublishAsync(object realtimeEvent, CancellationToken cancellationToken) =>
        hub.Clients.All.SendAsync(RealtimeHub.EventName, realtimeEvent, cancellationToken);
}
