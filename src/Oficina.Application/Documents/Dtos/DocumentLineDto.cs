namespace Oficina.Application.Documents.Dtos;

public sealed record DocumentLineDto(string Name, decimal Quantity, decimal UnitPrice, decimal Total);
