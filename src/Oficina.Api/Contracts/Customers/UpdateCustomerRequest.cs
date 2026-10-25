namespace Oficina.Api.Contracts.Customers;

public sealed record UpdateCustomerRequest(
    string Name,
    string? Email,
    string Phone,
    AddressRequest? Address,
    MessagingConsentRequest? MessagingConsent = null);
