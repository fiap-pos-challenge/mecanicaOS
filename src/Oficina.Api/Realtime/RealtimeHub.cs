using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Oficina.Api.Authorization;

namespace Oficina.Api.Realtime;

[Authorize(Policy = Policies.Authenticated)]
public sealed class RealtimeHub : Hub
{
    public const string Route = "/api/hubs/realtime";
    public const string EventName = "event";
}
