using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Enums;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Domain.Customers.Entities;

public sealed class Customer : AggregateRoot
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 160;

    private Customer(Guid id, string name, Document document, DocumentType documentType, Email? email, Phone phone)
        : base(id)
    {
        Name = name;
        Document = document;
        DocumentType = documentType;
        Email = email;
        Phone = phone;
    }

    public string Name { get; private set; }

    public Document Document { get; private set; }

    public DocumentType DocumentType { get; private set; }

    public Email? Email { get; private set; }

    public Phone Phone { get; private set; }

    public Address? Address { get; private set; }

    public bool ConsentWhatsApp { get; private set; }

    public bool ConsentEmail { get; private set; }

    public DateTimeOffset? ConsentUpdatedAt { get; private set; }

    public static Result<Customer> Create(string name, Document document, Email? email, Phone phone, Address? address)
    {
        if (!IsValidName(name))
        {
            return CustomerErrors.InvalidName;
        }

        var customer = new Customer(Guid.CreateVersion7(), name.Trim(), document, document.Type, email, phone);
        customer.Address = address;
        return customer;
    }

    public Result ChangeContact(string name, Email? email, Phone phone)
    {
        if (!IsValidName(name))
        {
            return CustomerErrors.InvalidName;
        }

        Name = name.Trim();
        Email = email;
        Phone = phone;
        return Result.Success();
    }

    public void ChangeAddress(Address? address) => Address = address;

    public Result ChangeMessagingConsent(bool whatsApp, bool email, DateTimeOffset now)
    {
        if (email && Email is null)
        {
            return CustomerErrors.EmailConsentWithoutEmail;
        }

        if (whatsApp == ConsentWhatsApp && email == ConsentEmail)
        {
            return Result.Success();
        }

        ConsentWhatsApp = whatsApp;
        ConsentEmail = email;
        ConsentUpdatedAt = now;
        return Result.Success();
    }

    private static bool IsValidName(string name) =>
        !string.IsNullOrWhiteSpace(name) && name.Trim().Length is >= NameMinLength and <= NameMaxLength;
}
