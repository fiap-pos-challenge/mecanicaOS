namespace Oficina.Domain.Quotes.Entities;

public sealed record EstimateLine(Guid Id, string Kind, Guid ItemId, string Name, decimal UnitPrice, decimal Quantity, string Source, bool Fractional);
