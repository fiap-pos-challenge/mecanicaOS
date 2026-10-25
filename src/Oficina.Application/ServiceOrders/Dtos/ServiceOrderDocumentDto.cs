namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record ServiceOrderDocumentDto(string Kind, string Code, DateTimeOffset IssuedAt);
