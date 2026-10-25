namespace Oficina.Api.Contracts.ServiceOrders;

public sealed record SearchServiceOrdersRequest(string? LicensePlate, string? Status, int? Page, int? PageSize);
