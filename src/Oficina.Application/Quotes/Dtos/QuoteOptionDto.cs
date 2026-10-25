namespace Oficina.Application.Quotes.Dtos;

public sealed record QuoteOptionDto(string Kind, Guid ItemId, string Name, string? Detail, decimal UnitPrice, bool Fractional, QuoteOptionStockDto? Stock, string? Reason);
