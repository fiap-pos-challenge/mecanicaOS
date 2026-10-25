namespace Oficina.Application.Quotes.Dtos;

public sealed record SendQuoteResultDto(QuoteDeliveryDto Delivery, string? WhatsAppUrl);
