namespace Oficina.Application.Catalog.Dtos;

public sealed record PriceChangeDto(string Field, decimal? OldValue, decimal NewValue, string ChangedBy, DateTimeOffset ChangedAt);
