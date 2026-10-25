namespace Oficina.Application.Quotes.Dtos;

public sealed record QuoteCustomerDto(Guid Id, string Name, string? MaskedPhone, string? MaskedEmail, bool ConsentWhatsApp, bool ConsentEmail, bool Loyal, bool FirstService, bool Fleet);
