using Oficina.Domain.Customers.Enums;

namespace Oficina.Application.Customers.Dtos;

public sealed record CustomerDto(
    Guid Id,
    string Name,
    string Document,
    DocumentType DocumentType,
    string? Email,
    string Phone,
    AddressDto? Address,
    MessagingConsentDto MessagingConsent,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
