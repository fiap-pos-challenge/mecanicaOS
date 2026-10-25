using Oficina.Domain.Catalog.Enums;

namespace Oficina.Api.Contracts.Catalog;

public sealed record PackageItemBody(CatalogItemKind Kind, Guid ItemId, decimal Quantity);
