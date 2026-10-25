using Oficina.Application.Customers.Dtos;

namespace Oficina.Api.Contracts.Customers;

public sealed record MessagingConsentRequest(bool WhatsApp, bool Email)
{
    public MessagingConsentInput ToInput() => new(WhatsApp, Email);
}
