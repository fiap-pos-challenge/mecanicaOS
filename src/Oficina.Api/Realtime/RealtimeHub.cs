using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Oficina.Api.Authorization;
using Oficina.Application.Abstractions.Realtime;

namespace Oficina.Api.Realtime;

[Authorize(Policy = Policies.Authenticated)]
public sealed class RealtimeHub : Hub
{
    public const string Route = "/api/hubs/realtime";
    public const string EventName = "event";
}

public sealed class SignalRRealtimePublisher(IHubContext<RealtimeHub> hub) : IRealtimePublisher
{
    public Task PublishAsync(object realtimeEvent, CancellationToken cancellationToken) =>
        hub.Clients.All.SendAsync(RealtimeHub.EventName, realtimeEvent, cancellationToken);
}
