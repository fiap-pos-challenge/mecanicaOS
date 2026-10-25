using Oficina.Domain.Catalog.Enums;

namespace Oficina.Application.Catalog.Dtos;

public sealed record PackageItemDto(CatalogItemKind Kind, Guid ItemId, decimal Quantity, string Name, decimal UnitPrice);
