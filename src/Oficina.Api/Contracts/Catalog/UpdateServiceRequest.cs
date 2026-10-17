namespace Oficina.Api.Contracts.Catalog;

public sealed record UpdateServiceRequest(string Name, string? Description, decimal BasePrice, int EstimatedMinutes, decimal CommissionPercent, int WarrantyDays, IReadOnlyList<UsualPartRequest>? UsualParts = null);
