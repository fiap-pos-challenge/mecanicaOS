using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Inventory.Entities;

namespace Oficina.Application.Catalog.Mappers;

public static class PartMapper
{
    public static PartDto ToDto(this Part part, bool includeCost) => new(
        part.Id,
        part.Code.Value,
        part.Name,
        part.Type,
        part.UnitOfMeasure,
        part.UnitPrice.Amount,
        includeCost ? part.Cost.Amount : null,
        part.StockQuantity,
        part.MinimumStock,
        part.Supplier,
        part.Barcode,
        part.IsActive,
        part.CreatedAt,
        part.UpdatedAt);
}
