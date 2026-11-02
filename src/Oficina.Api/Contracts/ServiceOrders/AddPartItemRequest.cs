namespace Oficina.Api.Contracts.ServiceOrders;

public sealed record AddPartItemRequest(Guid PartId, decimal Quantity);
