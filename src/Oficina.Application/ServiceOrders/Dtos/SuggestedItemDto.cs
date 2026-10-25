namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record SuggestedItemDto(string Kind, Guid ItemId, string Name, decimal Confidence, string Reason);
