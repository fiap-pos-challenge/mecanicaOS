using Oficina.Domain.ServiceOrders.Enums;

namespace Oficina.Api.Contracts.ServiceOrders;

public sealed record TransitionRequest(ServiceOrderStatus To, int Version, string? Reason);
