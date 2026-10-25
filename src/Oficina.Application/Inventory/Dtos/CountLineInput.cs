namespace Oficina.Application.Inventory.Dtos;

public sealed record CountLineInput(Guid PartId, decimal? CountedQuantity);
