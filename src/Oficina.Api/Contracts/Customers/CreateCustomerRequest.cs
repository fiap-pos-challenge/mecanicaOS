namespace Oficina.Api.Contracts.Customers;

public sealed record CreateCustomerRequest(
    string Name,
    string Document,
    string? Email,
    string Phone,
    AddressRequest? Address,
    MessagingConsentRequest? MessagingConsent = null);
