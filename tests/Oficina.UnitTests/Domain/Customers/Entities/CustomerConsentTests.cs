using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.UnitTests.Domain.Customers.Entities;

public sealed class CustomerConsentTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    private static Customer Create(string? email) =>
        Customer.Create("Maria Souza", Document.Create("52998224725").Value, email is null ? null : Email.Create(email).Value, Phone.Create("11987654321").Value, null).Value;

    [Fact]
    public void NewCustomer_HasNoConsent()
    {
        var customer = Create("maria@exemplo.com");

        Assert.False(customer.ConsentWhatsApp);
        Assert.False(customer.ConsentEmail);
        Assert.Null(customer.ConsentUpdatedAt);
    }

    [Fact]
    public void ChangeMessagingConsent_StampsTheDateOnlyWhenSomethingChanges()
    {
        var customer = Create("maria@exemplo.com");

        Assert.True(customer.ChangeMessagingConsent(true, true, Now).IsSuccess);
        Assert.True(customer.ChangeMessagingConsent(true, true, Now.AddDays(1)).IsSuccess);

        Assert.True(customer.ConsentWhatsApp);
        Assert.True(customer.ConsentEmail);
        Assert.Equal(Now, customer.ConsentUpdatedAt);
        customer.ChangeMessagingConsent(false, true, Now.AddDays(2));
        Assert.Equal(Now.AddDays(2), customer.ConsentUpdatedAt);
    }

    [Fact]
    public void ChangeMessagingConsent_RejectsEmailConsentWithoutEmail()
    {
        var customer = Create(null);

        Assert.Equal(CustomerErrors.EmailConsentWithoutEmail, customer.ChangeMessagingConsent(true, true, Now).Error);
        Assert.False(customer.ConsentWhatsApp);
    }
}
