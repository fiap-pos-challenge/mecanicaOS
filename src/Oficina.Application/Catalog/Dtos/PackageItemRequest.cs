using Oficina.Domain.Catalog.Enums;

namespace Oficina.Application.Catalog.Dtos;

public sealed record PackageItemRequest(CatalogItemKind Kind, Guid ItemId, decimal Quantity);
