namespace Oficina.Application.Quotes.Dtos;

public sealed record QuoteLineDto(Guid Id, string Kind, Guid ItemId, string Name, decimal UnitPrice, decimal Quantity, string Source, bool Fractional);
