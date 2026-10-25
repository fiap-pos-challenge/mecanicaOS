using Oficina.Domain.Inventory.Enums;

namespace Oficina.Api.Contracts.Catalog;

public sealed record PartRequest(
    string Code,
    string Name,
    PartType Type,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal Cost,
    decimal MinimumStock,
    string? Supplier,
    string? Barcode);
