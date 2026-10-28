using Oficina.Domain.Inventory.Enums;

namespace Oficina.Api.Contracts.Inventory;

public sealed record UpdatePartRequest(string Name, PartType Type, string UnitOfMeasure, decimal UnitPrice, decimal MinimumStock);
