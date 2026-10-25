namespace Oficina.Application.Customers.Dtos;

public sealed record MessagingConsentDto(bool WhatsApp, bool Email, DateTimeOffset? UpdatedAt);
