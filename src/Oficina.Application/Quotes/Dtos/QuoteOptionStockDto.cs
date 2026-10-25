namespace Oficina.Application.Quotes.Dtos;

public sealed record QuoteOptionStockDto(decimal Available, int? LeadTimeDays, string? Supplier);
