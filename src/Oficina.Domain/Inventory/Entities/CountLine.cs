namespace Oficina.Domain.Inventory.Entities;

public sealed record CountLine(Guid PartId, decimal SystemQuantity, decimal? CountedQuantity);
