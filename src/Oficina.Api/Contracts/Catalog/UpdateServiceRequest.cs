namespace Oficina.Api.Contracts.Catalog;

public sealed record UpdateServiceRequest(string Name, string? Description, decimal BasePrice, int EstimatedMinutes);
