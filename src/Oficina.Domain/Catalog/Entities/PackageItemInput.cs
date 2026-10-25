using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.Domain.Catalog.Entities;

public sealed record PackageItemInput(CatalogItemKind Kind, Guid ItemId, Quantity Quantity);
