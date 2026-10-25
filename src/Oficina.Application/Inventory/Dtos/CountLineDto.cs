namespace Oficina.Application.Inventory.Dtos;

public sealed record CountLineDto(Guid PartId, string Code, string Name, string Type, string UnitOfMeasure, decimal SystemQuantity, decimal? CountedQuantity);
