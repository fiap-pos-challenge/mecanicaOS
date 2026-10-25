namespace Oficina.Api.Contracts.ServiceOrders;

public sealed record PhotoRequest(string? ClientId, string FileName, string ContentType, string DataUrl);
