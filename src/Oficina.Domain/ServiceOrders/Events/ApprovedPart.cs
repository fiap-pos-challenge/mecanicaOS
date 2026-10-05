namespace Oficina.Domain.ServiceOrders.Events;

public sealed record ApprovedPart(Guid PartId, decimal Quantity);
