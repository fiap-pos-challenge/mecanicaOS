using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.ValueObjects;
using Riok.Mapperly.Abstractions;

namespace Oficina.Application.Inventory.Mappers;

[Mapper]
public static partial class PartMapper
{
    [MapperIgnoreSource(nameof(Part.DomainEvents))]
    [MapperIgnoreSource(nameof(Part.DeletedAt))]
    [MapperIgnoreSource(nameof(Part.IsDeleted))]
    [MapperIgnoreSource(nameof(Part.StockMovements))]
    public static partial PartDto ToDto(this Part part);

    [MapperIgnoreSource(nameof(Part.DomainEvents))]
    [MapperIgnoreSource(nameof(Part.DeletedAt))]
    [MapperIgnoreSource(nameof(Part.IsDeleted))]
    public static partial PartDetailDto ToDetailDto(this Part part);

    [MapperIgnoreSource(nameof(StockMovement.PartId))]
    [MapperIgnoreSource(nameof(StockMovement.DeletedAt))]
    [MapperIgnoreSource(nameof(StockMovement.IsDeleted))]
    [MapperIgnoreSource(nameof(StockMovement.UpdatedAt))]
    private static partial StockMovementDto ToDto(this StockMovement movement);

    private static string PartCodeToString(PartCode code) => code.Value;

    private static decimal MoneyToDecimal(Money money) => money.Amount;

    private static decimal QuantityToDecimal(Quantity quantity) => quantity.Value;
}
