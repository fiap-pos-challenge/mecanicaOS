namespace Oficina.Application.Quotes.Dtos;

public sealed record QuoteDraftItemInput(string Kind, Guid ItemId, decimal Quantity, decimal? UnitPrice, string? Source);
