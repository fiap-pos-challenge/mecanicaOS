namespace Oficina.Api.Contracts.ServiceOrders;

public sealed record AddServiceItemRequest(Guid ServiceId, int Quantity);
