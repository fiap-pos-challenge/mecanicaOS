namespace Oficina.Domain.Documents.Entities;

public sealed record DocumentLine(string Name, decimal Quantity, decimal UnitPrice, decimal Total);
