namespace Oficina.Api.Contracts.Catalog;

public sealed record CreateServiceRequest(string Name, string? Description, decimal BasePrice, int EstimatedMinutes);
